using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Orleans.Runtime.Configuration;

namespace Orleans.Runtime
{
    internal abstract class AsynchQueueAgent<T> : AsynchAgent, IDisposable where T : IOutgoingMessage
    {
        private readonly IMessagingConfiguration config;
        private ActionBlock<T> requestQueue;
        private QueueTrackingStatistic queueTracking;

        protected AsynchQueueAgent(string nameSuffix, IMessagingConfiguration cfg)
            : base(nameSuffix)
        {
            config = cfg;
            requestQueue = new ActionBlock<T>(request =>
            {
#if TRACK_DETAILED_STATS
                if (StatisticsCollector.CollectQueueStats)
                {
                    queueTracking.OnDeQueueRequest(request);
                }
                if (StatisticsCollector.CollectThreadTimeTrackingStats)
                {
                    threadTracking.OnStartProcessing();
                }
#endif
                Process(request);

#if TRACK_DETAILED_STATS
                if (StatisticsCollector.CollectThreadTimeTrackingStats)
                {
                    threadTracking.OnStopProcessing();
                    threadTracking.IncrementNumberOfProcessed();
                }
#endif
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                EnsureOrdered = false,
               // TaskScheduler = DedicatedThreadPoolTaskScheduler.Instance,
                MaxMessagesPerTask = 1
            });
            if (StatisticsCollector.CollectQueueStats)
            {
                queueTracking = new QueueTrackingStatistic(base.Name);
            }
        }

        public void QueueRequest(T request)
        {
#if TRACK_DETAILED_STATS
            if (StatisticsCollector.CollectQueueStats)
            {
                queueTracking.OnEnQueueRequest(1, requestQueue.Count, request);
            }
#endif
            requestQueue.Post(request);
        }

        protected abstract void Process(T request);

        protected override void Run()
        {
#if TRACK_DETAILED_STATS
            if (StatisticsCollector.CollectThreadTimeTrackingStats)
            {
                threadTracking.OnStartExecution();
                queueTracking.OnStartExecution();
            }
#endif
            try
            {
                requestQueue.Completion.Wait();
            }
            catch (AggregateException)
            {
                // run was cancelled
            }
            catch (ObjectDisposedException)
            {
            }

#if TRACK_DETAILED_STATS
               if (StatisticsCollector.CollectThreadTimeTrackingStats)
                 {
                    threadTracking.OnStopExecution();
                    queueTracking.OnStopExecution();
                }
#endif
        }

        public override void Stop()
        {
#if TRACK_DETAILED_STATS
            if (StatisticsCollector.CollectThreadTimeTrackingStats)
            {
                threadTracking.OnStopExecution();
            }
#endif
            requestQueue.Complete();
            base.Stop();
        }

        public virtual int Count
        {
            get
            {
                return requestQueue.InputCount;
            }
        }

#region IDisposable Members

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            
#if TRACK_DETAILED_STATS
            if (StatisticsCollector.CollectThreadTimeTrackingStats)
            {
                threadTracking.OnStopExecution();
            }
#endif
            base.Dispose(disposing);

            if (requestQueue != null)
            {
                requestQueue = null;
            }
        }

#endregion
    }
}
