using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkGrainInterfaces.MapReduce;
using BenchmarkGrains.MapReduce;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.TestingHost;
using System.Diagnostics;
using System.Management;

namespace Benchmarks.MapReduce
{
    public class MapReduceBenchmark
    {
        private static TestCluster _host;
        private readonly int _intermediateStagesCount = 15;
        private readonly int _pipelineParallelization = 4;
        private readonly int _repeats = 50000;
        private int _currentRepeat = 0;

        [Setup]
        public void BenchmarkSetup()
        {
            var options = new TestClusterOptions(1);
            options.ExtendedFallbackOptions.TraceToConsole = false;
            options.ClusterConfiguration.ApplyToAllNodes(c => c.DefaultTraceLevel = Severity.Warning);
            _host = new TestCluster(options);
            _host.Deploy();
        }

        [Benchmark]
        public async Task Bench()
        {
            List<string> Sockets = new List<string>();

            int PhysicalCPU = 0;

            int LogicalCPU = 0;


            //Create WMI Class

            ManagementClass mc = new ManagementClass("Win32_Processor");

            //Populate class with Processor objects

            ManagementObjectCollection moc = mc.GetInstances();


            //Iterate through logical processors

            foreach (ManagementObject mo in moc)

            {

                LogicalCPU++;


                string SocketDesignation = mo.Properties["SocketDesignation"].Value.ToString();


                //We will count the unique SocketDesignations to find

                //the number of physical CPUs in the system.

                if (!Sockets.Contains(SocketDesignation))

                {

                    Sockets.Add(SocketDesignation);

                }

            }


            PhysicalCPU = Sockets.Count;


            Console.WriteLine(LogicalCPU + " logical CPUs detected.");

            Console.WriteLine(PhysicalCPU + " physical CPUs detected.");


            //Are there more logical than physical cpus?

            //If so, obviously we are hyperthreading.

            if (LogicalCPU > PhysicalCPU)

            {

                Console.WriteLine("HyperThreading is enabled.");

            }
            var stopWatch = Stopwatch.StartNew();
            var pipelines = Enumerable
                .Range(0, this._pipelineParallelization)
                .AsParallel()
                .WithDegreeOfParallelism(4)
                .Select(async i =>
                {
                    await BenchCore();
                });

            await Task.WhenAll(pipelines); var messages = _repeats * (_intermediateStagesCount + 2) * 2 + _repeats;
            Console.WriteLine($"Messages: {messages.ToString()}");

            Console.WriteLine($"Throughput: {((float)messages / stopWatch.ElapsedMilliseconds) * 1000} msg per second");
        }

        public void Teardown()
        {
            _host.StopAllSilos();
        }

        private async Task BenchCore()
        {
            List<Task> initializationTasks = new List<Task>();
            var mapper = _host.GrainFactory.GetGrain<ITransformGrain<string, List<string>>>(Guid.NewGuid());
            initializationTasks.Add(mapper.Initialize(new MapProcessor()));
            var reducer =
                _host.GrainFactory.GetGrain<ITransformGrain<List<string>, Dictionary<string, int>>>(Guid.NewGuid());
            initializationTasks.Add(reducer.Initialize(new ReduceProcessor()));

            // used for imitation of complex processing pipelines
            var intermediateGrains = Enumerable
                .Range(0, this._intermediateStagesCount)
                .Select(i =>
                {
                    var intermediateProcessor =
                        _host.GrainFactory.GetGrain<ITransformGrain<Dictionary<string, int>, Dictionary<string, int>>>
                            (Guid.NewGuid());
                    initializationTasks.Add(intermediateProcessor.Initialize(new EmptyProcessor()));
                    return intermediateProcessor;
                });

            initializationTasks.Add(mapper.LinkTo(reducer));
            var collector = _host.GrainFactory.GetGrain<IBufferGrain<Dictionary<string, int>>>(Guid.NewGuid());
            using (var e = intermediateGrains.GetEnumerator())
            {
                ITransformGrain<Dictionary<string, int>, Dictionary<string, int>> previous = null;
                if (e.MoveNext())
                {
                    initializationTasks.Add(reducer.LinkTo(e.Current));
                    previous = e.Current;
                }

                while (e.MoveNext())
                {
                    initializationTasks.Add(previous.LinkTo(e.Current));
                    previous = e.Current;
                }

                initializationTasks.Add(previous.LinkTo(collector));
            }

            await Task.WhenAll(initializationTasks);

            List<Dictionary<string, int>> resultList = new List<Dictionary<string, int>>();

            while (Interlocked.Increment(ref this._currentRepeat) < this._repeats)
            {
                await mapper.SendAsync(this._text);
                while (!resultList.Any() || resultList.First().Count < 1) // rough way of checking of pipeline completition.
                {
                    resultList = await collector.ReceiveAll();
                }
            }
        }

        private string _text = @"Historically";
    }

}