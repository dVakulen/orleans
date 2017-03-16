using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Orleans.Runtime
{
    internal class RuntimeContext
    {
        public TaskScheduler Scheduler { get;  set; }
        public ISchedulingContext ActivationContext { get; private set; }
        private int ExecutionDepth;

        [ThreadStatic]
        private static RuntimeContext context;
        public static RuntimeContext Current 
        { 
            get { return context; } 
            set { context = value; }
        }

        internal static ISchedulingContext CurrentActivationContext
        {
            get { return RuntimeContext.Current != null ? RuntimeContext.Current.ActivationContext : null; }
        }

        internal static void InitializeThread(TaskScheduler scheduler)
        {
            // There seems to be an implicit coupling of threads and contexts here that may be fragile. 
            // E.g. if InitializeThread() is mistakenly called on a wrong thread, would that thread be considered a worker pool thread from that point on? 
            // Is there a better/safer way to identify worker threads? 
            if (context != null && scheduler != null)
            {
                throw new InvalidOperationException("RuntimeContext.Current has already been initialized for this thread.");
            }
            context = new RuntimeContext {Scheduler = scheduler};
        }

        internal static void InitializeMainThread()
        {
            context = new RuntimeContext {Scheduler = null};
        }

        internal static void SetExecutionContext(ISchedulingContext shedContext, TaskScheduler scheduler, bool justEnsure) // todo: remove ensure
        {
            if (context == null) throw new InvalidOperationException("SetExecutionContext called on unexpected non-WorkerPool thread");
            if (context.ActivationContext != null)
            {
                if (context.ActivationContext != shedContext)
                {
                    throw new Exception("SetExecutionContext from different activation");
                }
                else
                {
                    if (!justEnsure)
                    {
                        context.ExecutionDepth++;
                    }
                }
            }
            else
            {
                context.ActivationContext = shedContext;
                context.Scheduler = scheduler;
            }
        }

        public Stack<string> ContextResetters = new Stack<string>(); 
        internal static void ResetExecutionContext()
        {
            if (context.ContextResetters.Count > 10)
            {
                context.ContextResetters.Pop();
            }
            if (context.ExecutionDepth > 0)
            {
                context.ExecutionDepth--;
            }
            else
            {
                context.ActivationContext = null;
                context.Scheduler = null;
            }
            context.ContextResetters.Push(new StackTrace().ToString());
        }

        public override string ToString()
        {
            return String.Format("RuntimeContext: ActivationContext={0}, Scheduler={1}", 
                ActivationContext != null ? ActivationContext.ToString() : "null",
                Scheduler != null ? Scheduler.ToString() : "null");
        }
    }
}
