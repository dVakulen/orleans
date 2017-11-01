using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Orleans.Runtime.Configuration;

namespace Orleans.Runtime
{
    // queue services? where to move the BlockingCollection ?
    // instead of accumulating work items to do in local collections - lambdas with it
    // will be storred in execution service, thus only 1 mention of BC should remain
    internal abstract class AsynchQueueAgent<T> : AsynchAgent where T : IOutgoingMessage
    {
        private BlockingCollection<T> requestQueue;
        private QueueTrackingStatistic queueTracking;

        protected AsynchQueueAgent(ExecutorService executorService, string nameSuffix, ILoggerFactory loggerFactory)
            : base(executorService, nameSuffix, loggerFactory)
        {
            requestQueue = new BlockingCollection<T>();
            if (StatisticsCollector.CollectQueueStats)
            {
                queueTracking = new QueueTrackingStatistic(base.Name);
            }
        }

//        public override void Start()
//        {
//            // todo: submit to executor service? 
//        }

        public void QueueRequest(T request)
        {
            if (requestQueue==null)
            {
                return;
            }

#if TRACK_DETAILED_STATS
            if (StatisticsCollector.CollectQueueStats)
            {
                queueTracking.OnEnQueueRequest(1, requestQueue.Count, request);
            }
#endif

            requestQueue.Add(request);
        }

        protected abstract void Process(T request);

        // ???? wount be needed
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
                RunNonBatching();
            }
            finally
            {
#if TRACK_DETAILED_STATS
                if (StatisticsCollector.CollectThreadTimeTrackingStats)
                {
                    threadTracking.OnStopExecution();
                    queueTracking.OnStopExecution();
                }
#endif
            }
        }


        protected void RunNonBatching()
        {            
            while (true)
            {
                if (Cts == null || Cts.IsCancellationRequested)
                {
                    return;
                }
                T request;
                try
                {
                    request = requestQueue.Take();
                }
                catch (InvalidOperationException)
                {
                    Log.Info(ErrorCode.Runtime_Error_100312, "Stop request processed");
                    break;
                }
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
            }
        }

        public override void Stop()
        {
#if TRACK_DETAILED_STATS
            if (StatisticsCollector.CollectThreadTimeTrackingStats)
            {
                threadTracking.OnStopExecution();
            }
#endif
            requestQueue?.CompleteAdding();
            base.Stop();
        }

        protected void DrainQueue(Action<T> action)
        {
            T request;
            while (requestQueue.TryTake(out request))
            {
                action(request);
            }
        }

        public virtual int Count
        {
            get
            {
                return requestQueue.Count;
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

            requestQueue?.Dispose();
            requestQueue = null;
        }

        #endregion
    }
}
