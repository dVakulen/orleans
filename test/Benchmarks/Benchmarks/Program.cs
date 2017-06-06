using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Running;
using Benchmarks.MapReduce;
using Benchmarks.Serialization;
using OrleansBenchmarks.MapReduce;

namespace Benchmarks
{
    class Program
    {
        private static readonly Dictionary<string, Action> _benchmarks = new Dictionary<string, Action>
        {
            ["TimerWheel"] = () =>
            {
               // var summary = BenchmarkRunner.Run<TimerWheelBenchmark>();
            },
            ["MapReduce"] = () =>
            {
                RunBenchmark(
                "Running MapReduce benchmark", 
                () =>
                {
                    var mapReduceBenchmark = new MapReduceBenchmark();
                    mapReduceBenchmark.BenchmarkSetup();
                    return mapReduceBenchmark;
                },
                     benchmark =>
                     {
                         benchmark.Bench().Wait();
                     },
                benchmark => benchmark.Teardown());
            },
            ["Serialization"] = () =>
            {
            }
        };

        // requires benchmark name or 'All' word as first parameter
        static void Main(string[] args)
        {
            for (int i = 0; i < 7; i++)
            {
                _benchmarks["MapReduce"]();
            }
            Console.ReadLine();
        }

        private static void RunBenchmark<T>(string name, Func<T> init, Action<T> benchmarkAction, Action<T> tearDown)
        {
            Console.WriteLine(name);
            var bench = init();
            var stopWatch = Stopwatch.StartNew();
            benchmarkAction(bench);
            Console.WriteLine($"Elapsed milliseconds: {stopWatch.ElapsedMilliseconds}");
            tearDown(bench);
        }
    }
}
