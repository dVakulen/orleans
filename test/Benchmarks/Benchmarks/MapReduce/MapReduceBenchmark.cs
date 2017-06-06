using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkGrainInterfaces.MapReduce;
using BenchmarkGrains.MapReduce;
using Orleans;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.TestingHost;
using OrleansBenchmarkGrains.MapReduce;

namespace OrleansBenchmarks.MapReduce
{
    public class MapReduceBenchmark
    {
        private static TestCluster _host;
        private readonly int _intermediateStagesCount = 15;
        private readonly int _pipelineParallelization =Environment.ProcessorCount;
        private readonly int _repeats = 200000;// 2500000;
        private int _currentRepeat = 0;

        [Setup]
        public void BenchmarkSetup()
        {
            var options = new TestClusterOptions(1);
          options.ExtendedFallbackOptions.TraceToConsole = false;
           options.ClusterConfiguration.ApplyToAllNodes(c => c.DefaultTraceLevel = Severity.Info);
            _host = new TestCluster(options);
            _host.Deploy();
        }

        [Benchmark]
        public async Task Bench()
        {
            var stopWatch = Stopwatch.StartNew();
            var pipelines = Enumerable
                .Range(0, _pipelineParallelization) // 4  Environment.ProcessorCount
				.AsParallel()
                .WithDegreeOfParallelism(_pipelineParallelization)
                .Select(async i =>
                {
                    await BenchCore();
                });

            await Task.WhenAll(pipelines);
            var messages = _repeats*(_intermediateStagesCount + 2) * 2 + _repeats;
            Console.WriteLine($"Messages: {messages.ToString()}");

            Console.WriteLine($"Throughput: {((float)messages / stopWatch.ElapsedMilliseconds) * 1000} msg per second");
        }

        public void Teardown()
        {
            _host.StopAllSilos();
        }

        private async Task BenchCore()
        {
            while (Interlocked.Increment(ref _currentRepeat) < _repeats)
            {
                List<Task> initializationTasks = new List<Task>();
                var mapper = _host.GrainFactory.GetGrain<ITransformGrain<string, List<string>>>(Guid.NewGuid());
                initializationTasks.Add(mapper.Initialize(new MapProcessor()));
                var reducer =
                    _host.GrainFactory.GetGrain<ITransformGrain<List<string>, Dictionary<string, int>>>(Guid.NewGuid());
                initializationTasks.Add(reducer.Initialize(new ReduceProcessor()));

                // used for imitation of complex processing pipelines
                var intermediateGrains = Enumerable
                    .Range(0, _intermediateStagesCount)
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
                int localRep = 0;
                while (Interlocked.Increment(ref _currentRepeat) < _repeats)
                {
                    localRep++;
                       await mapper.SendAsync(_text);
                    while (!resultList.Any() || resultList.First().Count < 1) // rough way of checking of pipeline completition.
                    {
                        Interlocked.Increment(ref wwqw);
                        resultList = await collector.ReceiveAll();
                    }
                }
            }
          //  Console.WriteLine("wE!!!" + wwqw);
        }
        private int wwqw;
        private string _text = @"Historically";
    }

}