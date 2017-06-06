//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using Orleans.Runtime.Configuration;

//namespace Orleans.Runtime
//{
//    internal abstract class AsynchQueueAgent<T> : AsynchAgent, IDisposable where T : IOutgoingMessage
//    {
//        private readonly IMessagingConfiguration config;
//        private ManualResetEvent Completion = new ManualResetEvent(false);
//        private QueueTrackingStatistic queueTracking;
//        private readonly WaitCallback requestHandler;

//        protected AsynchQueueAgent(string nameSuffix, IMessagingConfiguration cfg)
//            : base(nameSuffix)
//        {
//            config = cfg;
//            requestHandler = request =>
//            {
//#if TRACK_DETAILED_STATS
//                if (StatisticsCollector.CollectQueueStats)
//                {
//                    queueTracking.OnDeQueueRequest(request);
//                }
//                if (StatisticsCollector.CollectThreadTimeTrackingStats)
//                {
//                    threadTracking.OnStartProcessing();
//                }
//#endif
//                var b = new TimeTracker(nameSuffix).Track();
//                Process((T)request);
//                b.StopTrack();
//#if TRACK_DETAILED_STATS
//                if (StatisticsCollector.CollectThreadTimeTrackingStats)
//                {
//                    threadTracking.OnStopProcessing();
//                    threadTracking.IncrementNumberOfProcessed();
//                }
//#endif
//            };

//            if (StatisticsCollector.CollectQueueStats)
//            {
//                queueTracking = new QueueTrackingStatistic(base.Name);
//            }
//        }

//        public void QueueRequest(T request)
//        {
//#if TRACK_DETAILED_STATS
//            if (StatisticsCollector.CollectQueueStats)
//            {
//                queueTracking.OnEnQueueRequest(1, requestQueue.Count, request);
//            }
//#endif
//             OrleansThreadPool.QueueSystemWorkItem(requestHandler, request);
//        }

//        protected abstract void Process(T request);

//        protected override void Run()
//        {
//#if TRACK_DETAILED_STATS
//            if (StatisticsCollector.CollectThreadTimeTrackingStats)
//            {
//                threadTracking.OnStartExecution();
//                queueTracking.OnStartExecution();
//            }
//#endif
//            try
//            {
//                Completion.WaitOne();
//            }
//            catch (AggregateException)
//            {
//                // run was cancelled
//            }
//            catch (ObjectDisposedException)
//            {
//            }

//#if TRACK_DETAILED_STATS
//               if (StatisticsCollector.CollectThreadTimeTrackingStats)
//                 {
//                    threadTracking.OnStopExecution();
//                    queueTracking.OnStopExecution();
//                }
//#endif
//        }

//        public override void Stop()
//        {
//#if TRACK_DETAILED_STATS
//            if (StatisticsCollector.CollectThreadTimeTrackingStats)
//            {
//                threadTracking.OnStopExecution();
//            }
//#endif
//            Completion.Set();
//            base.Stop();
//        }

//        public virtual int Count
//        {
//            get
//            {//todo
//                return 0;
//            }
//        }

//#region IDisposable Members

//        protected override void Dispose(bool disposing)
//        {
//            if (!disposing) return;

//#if TRACK_DETAILED_STATS
//            if (StatisticsCollector.CollectThreadTimeTrackingStats)
//            {
//                threadTracking.OnStopExecution();
//            }
//#endif
//            base.Dispose(disposing);
//        }

//#endregion
//    }
//}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Orleans.Runtime.Configuration;

namespace Orleans.Runtime
{
    internal abstract class AsynchQueueAgent<T> : AsynchAgent, IDisposable where T : IOutgoingMessage
    {
        private readonly IMessagingConfiguration config;
        private BlockingCollection<T> requestQueue;
        private QueueTrackingStatistic queueTracking;

        protected AsynchQueueAgent(string nameSuffix, IMessagingConfiguration cfg)
            : base(nameSuffix)
        {
            config = cfg;
            requestQueue = new BlockingCollection<T>();
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
            requestQueue.Add(request);
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
                if (Cts.IsCancellationRequested)
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
            requestQueue.CompleteAdding();
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

            if (requestQueue != null)
            {
                requestQueue.Dispose();
                requestQueue = null;
            }
        }

        #endregion
    }
}