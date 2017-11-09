using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Orleans.Runtime.Configuration;
using Orleans.Threading;



/**
 * Provides default implementations of {@link ExecutorService}
 * execution methods. This class implements the {@code submit},
 * {@code invokeAny} and {@code invokeAll} methods using a
 * {@link RunnableFuture} returned by {@code newTaskFor}, which defaults
 * to the {@link FutureTask} class provided in this package.  For example,
 * the implementation of {@code submit(Runnable)} creates an
 * associated {@code RunnableFuture} that is executed and
 * returned. Subclasses may override the {@code newTaskFor} methods
 * to return {@code RunnableFuture} implementations other than
 * {@code FutureTask}.
 *
 * <p><b>Extension example</b>. Here is a sketch of a class
 * that customizes {@link ThreadPoolExecutor} to use
 * a {@code CustomTask} class instead of the default {@code FutureTask}:
 * <pre> {@code
 * public class CustomThreadPoolExecutor extends ThreadPoolExecutor {
 *
 *   static class CustomTask<V> implements RunnableFuture<V> {...}
 *
 *   protected <V> RunnableFuture<V> newTaskFor(Callable<V> c) {
 *       return new CustomTask<V>(c);
 *   }
 *   protected <V> RunnableFuture<V> newTaskFor(Runnable r, V v) {
 *       return new CustomTask<V>(r, v);
 *   }
 *   // ... add constructors, etc.
 * }}</pre>
 *
 * @since 1.5
 * @author Doug Lea
 */
// java's ExecutorService being reference, 
// with ThreadPool as ... 
// thus created stagedExecutorService (stagedThreadPool)
//public abstract class AbstractExecutorService implements ExecutorService
//{
// stagedExecutorService

namespace Orleans.Runtime
{
    // pr notes: -work is two fold: 1 step - extract threading logic and make it injectable dependency
    // 2 step - implement stage ( workers pool + queue)
    // exctracted frokm asynch agent logic could be consolidated with WorkerPoolTHread one, 
    // but for simplicity of this PR  it remained untouched

    // . 
    // concepts introduced:  ..
    // responsibilites moved : ..
    // in order to reduce cognitive load: pending renames for another PR: AsynchAgent -> stage definition
    interface IActionDescriptor
    {
        void Run();
    }

    // threadpool - ThreadPoolType.Fixed, sclaing, is ThreadpooBuilder needed?
    // convinient way to ensure compile time invariants, 
    interface IStageAttribute
    {
    }

    // approach - copy existing, get tests green, delete old.

    interface OrleansContextRequired : IStageAttribute { }
    //static class ActionFaultBehavior
    //{
    //    public interface IStageFaultBehavior : IStageAttribute // Crash the process if the agent faults
    //    {
    //    }

    //    public interface CrashOnFault : IStageFaultBehavior // Crash the process if the agent faults
    //    {
    //    }

    //    public interface RestartOnFault : IStageFaultBehavior // Restart the agent if it faults
    //    {
    //    }

    //    public interface IgnoreFault : IStageFaultBehavior // Allow the agent to stop if it faults, but take no other action (other than logging)
    //    {
    //    }
    //}


    interface IStageDefinition
    {
    }

    // stage - actions : 1 to many
    // due to number of entities to take new dependency for reducing churn
    // it's order in parameters list will be in case of presense of log factory  - right before it, 
    // not accounting it's importance to class functioning (for churn reducing)
    /**
   * Submits a value-returning task for execution and returns a
   * Future representing the pending results of the task. The
   * Future's {@code get} method will return the task's result upon
   * successful completion.
   *
   * <p>
   * If you would like to immediately block waiting
   * for a task, you can use constructions of the form
   * {@code result = exec.submit(aCallable).get();}
   *
   * <p>Note: The {@link Executors} class includes a set of methods
   * that can convert some other common closure-like objects,
   * for example, {@link java.security.PrivilegedAction} to
   * {@link Callable} form so they can be submitted.
   *
   * @param task the task to submit
   * @param <T> the type of the task's result
   * @return a Future representing pending completion of the task
   * @throws RejectedExecutionException if the task cannot be
   *         scheduled for execution
   * @throws NullPointerException if the task is null
   */
    //  <T> Future<T> submit(Callable<T> task); (ref - java ExecutorService)
    abstract class ExecutorService // not needed?  .. stage info requirement is leaking from below .. 
    {
        // returns stageExecutionHandle // agent.stop  used only for cts, handle not needed? 
        // or should the ExecutorService just provide means for job restarting?

        // passing stage and action info as generic parameters in comparison with passing 
        // as argument:as at each call point 
        // the values are static(does not change during execution), more clearly states this info immutability,
        // also helps to show class contract in class definition ( <IConcreteActionDescriptor> - ) 
        // todo: overload without TIActionDescriptor withc will use default
        // + overload  with argument as param? could be added later
        // predefined executors? 
        // should accept actionDescriptor instead of Action.
        // generics - enforcing contracts
      
        public abstract void Submit<TStage>(TStage stage, Action work) // TStage,typ
            where TStage : IStageDefinition;
    }

    abstract class StagesExecutionPlan
    {
        // could be changeable at runtime (adopting to load, etc)
        private Dictionary<Type, IStageExecutor> mapping { get; } 

        protected HashSet<Type> KnownStages { get; }

        public StagesExecutionPlan()
        {
            mapping = new Dictionary<Type, IStageExecutor>();
            KnownStages = GetKnownStages();
        }

        private HashSet<Type> GetKnownStages()
        {
            return new HashSet<Type>
            {
                typeof(ConcreteStageDefinition)
            };
        }

        // + list of known stages
        protected void Register<T>(IStageExecutor executor) where T : IStageDefinition
        {
            mapping.Add(typeof(T), executor);
        }

        // should be renamed into get executor.
        public void Dispatch<TStage>(TStage stage, Action workItem)
            where TStage : IStageDefinition
        {
            if (mapping.TryGetValue(typeof(TStage), out var correspondingExecutor))
            {

                correspondingExecutor.Execute<TStage>(stage, workItem);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(state =>
                {

                    try
                    {
                        workItem();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                });
                return;
                throw new Exception();
            }
        }
    }

    class ThreadPoolPerStageExecutionPlan : StagesExecutionPlan
    {
        // public Dictionary<Type, IStageExecutor> currentMapping = new Dictionary<Type, IStageExecutor>();

        public ThreadPoolPerStageExecutionPlan()
        {
            Register<ConcreteStageDefinition>(new LimitedСoncurrencyStageExecutor());
        }

        //  private readonly 
        //  protected override Dictionary<Type, IStageExecutor> currentMapping { get; }
    }

    


    class ConcreteStageDefinition : IStageDefinition
    {
    }
    
    class StagedExecutorService : ExecutorService // rename.? most likely only ExecutorService is to remain
    {
//        public StagedExecutorService(NodeConfiguration config)
//        {
//        }
//
//        public StagedExecutorService(ClientConfiguration config)
//        {
//        }

        // could be swappable at runtime
        private StagesExecutionPlan currentExecutionPlan = new ThreadPoolPerStageExecutionPlan();


//         class ConcreteStageExecutor : StageExecutor<ConcreteStageDescription>
//        {
//        }

        // overload with func returning promise isn't needed? 

        // returns stageExecutionHandle ? ensure number of concurrent runs constrain. 
        //public override void Submit<TStage, T>(Action work) // swap submit - dispatch 
        //{
        //    currentExecutionPlan.Dispatch<TStage, T>(work);
        //    throw new NotImplementedException();
        //}

        //  ThreadPool.QueueUserWorkItem()
        // this.currentExecutorService.submit
        // current impl: per stage worker pool with optional blocking queue
       
        public override void Submit<TStage>(TStage stage, Action work)
        {
            currentExecutionPlan.Dispatch(stage, work);
        }
    }

   

    //  "Enable execution engine config\switch"
    // there should be ability to partially switch stages implementations
    // in order to enable coarce - grained configuration based optimizations

    // AsynchAgent -  long running work. queue agents - many short-running workloads //FaultBehavior

    // move  threading related thing out of AsynchAgent
    // AsynchAgent - becomes more of a trait (StageDefinition)?, and its content moves into poolThread./ start stop able.
    // take work dispatcher as dependency
    // dispatcher accepts work items + stage definition
    // dispatcher has internal stages to executors mappings (plan)
    // current impl will be mapped to pools of adjusted workerPoolThreads
    //    internal abstract class SingleActionAsynchAgent: AsynchAgent
    //    {
    //        // need in empty ctors should be removed in following c# versions https://github.com/dotnet/csharplang/issues/806
    //        protected SingleActionAsynchAgent(ExecutorService executorService, string nameSuffix, ILoggerFactory loggerFactory) : base(executorService, nameSuffix, loggerFactory)
    //        {
    //        }
    //
    //        protected SingleActionAsynchAgent(ExecutorService executorService, ILoggerFactory loggerFactory) : base(executorService, loggerFactory)
    //        {
    //        }
    //
    //        //        protected override void Run<Q>()
    //        //            where  Q : IActionDescriptor
    //        //        {
    //        //            
    //        //        }
    //
    //        // what does start means? - Stage start 
    //
    //        // for SingleActionAsynchAgent - does it means it should single action? (looks like yes) 
    //        public override void Start()
    //        {
    ////            ApplyPartial((this, "") =>
    ////            {
    ////                executorService.SubmitqQ(this, null);
    ////            })
    ////            // consider submit typeof(this) as stage
    ////            executorService.SubmitqQ(this,   null);
    //            // 'type inference 
    //            executorService.SubmitW(this, GetAction());
    //        }
    //        // this is agent business logic
    //        // run - also action , so will have action descriptor
    //        //   / protected abstract void Run<T>() where T: IActionDescriptor;
    //        // todo: verify fit in inheritors
    //        // being called at stage start
    //        public abstract IActionDescriptor GetAction() ;  // { get; } //where T : 
    //        public static Func<TResult> ApplyPartial<T1, TResult>
    //            (Func<T1, TResult> function, T1 arg1)
    //        {
    //            return () => function(arg1);
    //        }
    //    }


    //
    //    internal abstract class DefaultAsynchAgent: DefaultActionAsynchAgent<DefaultAsynchAgent>
    //    {
    //        protected DefaultAsynchAgent(ExecutorService executorService, string nameSuffix, ILoggerFactory loggerFactory) : base(executorService, nameSuffix, loggerFactory)
    //        {
    //        }
    //
    //        protected DefaultAsynchAgent(ExecutorService executorService, ILoggerFactory loggerFactory) : base(executorService, loggerFactory)
    //        {
    //        }
    //    }

    //    internal abstract class AsynchAgent : StageWorkerThread, IStageDefinition, IDisposable
    //    {
    //        protected AsynchAgent(ExecutorService executorService, string nameSuffix, ILoggerFactory loggerFactory) : base(nameSuffix, loggerFactory)
    //        {
    //        }
    //
    //        protected AsynchAgent(ExecutorService executorService, ILoggerFactory loggerFactory) : base(loggerFactory)
    //        {
    //        }
    //    }

        

    interface ILimitedСoncurrencyStage : IStageAttribute
    {
        int MaximumConcurrencyLevel { get; }
    }

    internal abstract class AsynchAgent : IStageDefinition, ILimitedСoncurrencyStage, IDisposable
    //<TStage> :   where TStage : IStageDefinition// , for simplicity for now its not
    // it is actually single stage.. .
    //. Requires implementer to explicitly pass themselves so submit
    // on fault should be passed alongside concrete action 
    {  // asyncg agent- stage reference. 
        public enum FaultBehavior
        {
            CrashOnFault,   // Crash the process if the agent faults
            RestartOnFault, // Restart the agent if it faults
            IgnoreFault     // Allow the agent to stop if it faults, but take no other action (other than logging)
        }

        protected readonly ExecutorService executorService;
        protected CancellationTokenSource Cts;
        protected object Lockable;
        protected Logger Log;
        private readonly string type;
        protected FaultBehavior OnFault;

        // instance of AsynchAgent stage definition can have only 1  running thread, but there can be multiple definitions
        public int MaximumConcurrencyLevel => 1;
#if TRACK_DETAILED_STATS
        internal protected ThreadTrackingStatistic threadTracking;
#endif

        internal string Name { get; private set; }
        //   private Catalog Catalog => this.catalog ?? (this.catalog = this.ServiceProvider.GetRequiredService<Catalog>());
        // nameSuffix - maybe at some point should be removed.

        protected AsynchAgent(ExecutorService executorService, string nameSuffix, ILoggerFactory loggerFactory)
        {
            this.executorService = executorService;

            Cts = new CancellationTokenSource();
            var thisType = GetType();

            type = thisType.Namespace + "." + thisType.Name;
            if (type.StartsWith("Orleans.", StringComparison.Ordinal))
            {
                type = type.Substring(8);
            }
            if (!string.IsNullOrEmpty(nameSuffix))
            {
                Name = type + "/" + nameSuffix;
            }
            else
            {
                Name = type;
            }

            OnFault = FaultBehavior.IgnoreFault;
            Lockable = new object();
            Log = new LoggerWrapper(Name, loggerFactory);
#if TRACK_DETAILED_STATS
            if (StatisticsCollector.CollectThreadTimeTrackingStats)
            {
                threadTracking = new ThreadTrackingStatistic(Name);
            }
#endif
        }

        protected AsynchAgent(ExecutorService executorService, ILoggerFactory loggerFactory)
            : this(executorService, null, loggerFactory)
        {
        }

        private List<StageWorkerThread> wwww = new List<StageWorkerThread>();

   //     private ILimitedСoncurrencyStage _limitedСoncurrencyStageImplementation;

        // there action must be submitted to executor service
        // start of the stage execution.
        public virtual void Start()
        {
            Cts = new CancellationTokenSource();
            // todo: executor service should ensure concurrent run number guarantees
            // 
            executorService.Submit(this, Run);
        }

        //        public ThreadState State { get; private set; } = ThreadState.Unstarted;
        //        public virtual void Start()
        //        {
        //            lock (Lockable)
        //            {
        //                if (State == ThreadState.Running)
        //                {
        //                    return;
        //                }
        //
        //                if (State == ThreadState.Stopped)
        //                {
        //                    Cts = new CancellationTokenSource();
        //
        //                    t = new Thread(AgentThreadProc) { IsBackground = true, Name = this.Name };
        //                }
        //
        //                t.Start(this);
        //                State = ThreadState.Running;
        //            }
        //            if (Log.IsVerbose) Log.Verbose("Started asynch agent " + this.Name);
        //        }
        //
        //        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        //        public virtual void Stop()
        //        {
        //            try
        //            {
        //                lock (Lockable)
        //                {
        //                    if (State == ThreadState.Running)
        //                    {
        //                        State = ThreadState.StopRequested;
        //                        Cts.Cancel();
        //                        State = ThreadState.Stopped;
        //                    }
        //                }
        //            }
        //            catch (Exception exc)
        //            {
        //                // ignore. Just make sure stop does not throw.
        //                Log.Verbose("Ignoring error during Stop: {0}", exc);
        //            }
        //            Log.Verbose("Stopped agent");
        //        }


        protected abstract void Run();


        //                if (State == ThreadState.Stopped)
        //                {
        //                    Cts = new CancellationTokenSource();
        //                    t = new Thread(AgentThreadProc) { IsBackground = true, Name = this.Name };
        //                }
        // if(Log.IsVerbose) Log.Verbose("Started asynch agent " + this.Name);
        // }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public virtual void Stop()
        {
            // todo: StopRequested verify that it is not needed

            Cts.Cancel();
            Log.Verbose("Stopped agent");

        }

        //
        //        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        //        private static void AgentThreadProc(Object obj)// not neeeded
        //        {
        //            var agent = obj as AsynchAgent;
        //            if (agent == null)
        //            {
        //                throw new InvalidOperationException("Agent thread started with incorrect parameter type");
        //    }
        //
        //            try
        //            {
        //                LogStatus(agent.Log, "Starting AsyncAgent {0} on managed thread {1}", agent.Name, Thread.CurrentThread.ManagedThreadId);
        //    CounterStatistic.SetOrleansManagedThread(); // do it before using CounterStatistic.
        //                CounterStatistic.FindOrCreate(new StatisticName(StatisticNames.RUNTIME_THREADS_ASYNC_AGENT_PERAGENTTYPE, agent.type)).Increment();
        //    CounterStatistic.FindOrCreate(StatisticNames.RUNTIME_THREADS_ASYNC_AGENT_TOTAL_THREADS_CREATED).Increment();
        //
        //    agent.Run();
        //            }
        //            finally
        //            {
        //                CounterStatistic.FindOrCreate(new StatisticName(StatisticNames.RUNTIME_THREADS_ASYNC_AGENT_PERAGENTTYPE, agent.type)).DecrementBy(1);
        //agent.Log.Info(ErrorCode.Runtime_Error_100328, "Stopping AsyncAgent {0} that runs on managed thread {1}", agent.Name, Thread.CurrentThread.ManagedThreadId);
        //            }
        //        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Cts != null)
            {
                Cts.Dispose();
                Cts = null;
            }
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }

    }
}