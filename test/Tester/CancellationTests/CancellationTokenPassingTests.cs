using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.TestingHost;
using Orleans.Threading;
using UnitTests.GrainInterfaces;
using Xunit;
using UnitTests.Tester;

namespace UnitTests.MembershipTests
{
    public class CancellationTokenPassingTests : HostedTestClusterPerTest
    {
        private double[] cancellationDelaysInMS = { 0, 10, 50, 500 };

        public override TestingSiloHost CreateSiloHost()
        {
            return new TestingSiloHost(new TestingSiloOptions
            {
                StartFreshOrleans = false,
                StartPrimary = true,
                StartSecondary = true,
                AdjustConfig = config =>
                {
                    config.Globals.DefaultPlacementStrategy = "ActivationCountBasedPlacement";
                }
            });
        }

        [Fact, TestCategory("Functional")]
        public async Task GrainTaskCancellation()
        {
            foreach (var delay in cancellationDelaysInMS.Select(TimeSpan.FromMilliseconds))
            {
                var grain = HostedCluster.GrainFactory.GetGrain<ILongRunningTaskGrain<bool>>(Guid.NewGuid());
                var tcs = new GrainCancellationTokenSource();
                var wait = grain.LongWait(tcs.GrainCancellationToken, TimeSpan.FromSeconds(10));
                await Task.Delay(delay);
                await tcs.Cancel();
                await Assert.ThrowsAsync<TaskCanceledException>(() => wait);
            }
        }

        [Fact, TestCategory("Functional")]
        public async Task PreCancelledTokenPassing()
        {
            var grain = HostedCluster.GrainFactory.GetGrain<ILongRunningTaskGrain<bool>>(Guid.NewGuid());
            var tcs = new GrainCancellationTokenSource();
            await tcs.Cancel();
            var wait = grain.LongWait(tcs.GrainCancellationToken, TimeSpan.FromSeconds(10));
            await Assert.ThrowsAsync<TaskCanceledException>(() => wait);
        }

        [Fact, TestCategory("Functional")]
        public async Task CancellationTokenCallbacksExecutionContext()
        {
            var grain = HostedCluster.GrainFactory.GetGrain<ILongRunningTaskGrain<bool>>(Guid.NewGuid());
            var tcs = new GrainCancellationTokenSource();
            var wait = grain.CancellationTokenCallbackResolve(tcs.GrainCancellationToken);
            await Task.Delay(1000);
            await tcs.Cancel();
            var result = await wait;
            Assert.Equal(true, result);
        }

        [Fact, TestCategory("Functional")]
        public async Task InterSiloClientCancellationTokenPassing()
        {
            foreach (var delay in cancellationDelaysInMS.Select(TimeSpan.FromMilliseconds))
            {
                var grains = await GetGrains<bool>();
                var grain = grains.Item1;
                var target = grains.Item2;
                var tcs = new GrainCancellationTokenSource();
                var wait = grain.CallOtherLongRunningTask(target, tcs.GrainCancellationToken, TimeSpan.FromSeconds(10));
                await Task.Delay(delay);
                await tcs.Cancel();
                await Assert.ThrowsAsync<TaskCanceledException>(() => wait);
            }
        }

        [Fact, TestCategory("Functional")]
        public async Task InterSiloGrainCancellation()
        {
            await GrainGrainCancellation(true);
        }

        [Fact, TestCategory("Functional")]
        public async Task InSiloGrainCancellation()
        {
            await GrainGrainCancellation(false);
        }

        private async Task GrainGrainCancellation(bool interSilo)
        {
            foreach (var delay in cancellationDelaysInMS)
            {
                var grains = await GetGrains<bool>(interSilo);
                var grain = grains.Item1;
                var target = grains.Item2;
                var wait = grain.CallOtherLongRunningTaskWithLocalToken(target, TimeSpan.FromSeconds(10),
                    TimeSpan.FromMilliseconds(delay));
                await Assert.ThrowsAsync<TaskCanceledException>(() => wait);
            }
        }

        private async Task<Tuple<ILongRunningTaskGrain<T1>, ILongRunningTaskGrain<T1>>> GetGrains<T1>(bool placeOnDifferentSilos = true)
        {
            var grain = HostedCluster.GrainFactory.GetGrain<ILongRunningTaskGrain<T1>>(Guid.NewGuid());
            var instanceId = await grain.GetRuntimeInstanceId();
            var target = HostedCluster.GrainFactory.GetGrain<ILongRunningTaskGrain<T1>>(Guid.NewGuid());
            var targetInstanceId = await target.GetRuntimeInstanceId();
            var retriesCount = 0;
            var retriesLimit = 7;
            if (placeOnDifferentSilos)
            {
                while (instanceId.Equals(targetInstanceId))
                {
                    if(retriesCount >= retriesLimit) throw new Exception("Could not place grains on different silos");
                    target = HostedCluster.GrainFactory.GetGrain<ILongRunningTaskGrain<T1>>(Guid.NewGuid());
                    targetInstanceId = await target.GetRuntimeInstanceId();
                    retriesCount++;
                }
            }
            else
            {
                while (!instanceId.Equals(targetInstanceId))
                {
                    if (retriesCount >= retriesLimit) throw new Exception("Could not place grains on same silo");
                    target = HostedCluster.GrainFactory.GetGrain<ILongRunningTaskGrain<T1>>(Guid.NewGuid());
                    targetInstanceId = await target.GetRuntimeInstanceId();
                    retriesCount++;
                }
            }

            return new Tuple<ILongRunningTaskGrain<T1>, ILongRunningTaskGrain<T1>>(grain, target);
        }
    }
}
