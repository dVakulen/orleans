using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Runtime.Configuration;

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
    interface IActionAttribute
    {
    }

    // approach - copy existing, get tests green, delete old.

    interface OrleansContextRequired : IActionAttribute { }
    static class ActionFaultBehavior
    {
        public interface CrashOnFault : IActionAttribute // Crash the process if the agent faults
        {
        }

        public interface RestartOnFault : IActionAttribute // Restart the agent if it faults
        {
        }

        public interface IgnoreFault : IActionAttribute // Allow the agent to stop if it faults, but take no other action (other than logging)
        {
        }
    }

    static class StandartActionDescriptors
    {
       public class CrashOnFaultActionDescriptor : IActionDescriptor, ActionFaultBehavior.CrashOnFault
        {
            
        }
    }

    interface IStageDefinition
    {
    }

    // stage - actions : 1 to many
    // due to number of entities to take new dependency for reducing churn
    // it's order in parameters list will be in case of presense of log factory  - right before it, 
    // not accounting it's importance to class functioning (for churn reducing)

    //  class StageExecutionHandle
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
        public abstract void Submit<TStage, T>(TStage stage, Action work) // TStage,typ
            where TStage : IStageDefinition
            where T : IActionDescriptor;
        public abstract void SubmitW<TStage, T>(TStage stage, T work) // TStage,typ
            where TStage : IStageDefinition
            where T : IActionDescriptor;
        public abstract void SubmitqQ<TStage>(TStage stage, Action work) // TStage,typ
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

        public void Dispatch<TStage, T>(Action workItem)
            where TStage : IStageDefinition
            where T : IActionDescriptor
        {
            if (mapping.TryGetValue(typeof(TStage), out var correspondingExecutor))
            {
                correspondingExecutor.Execute<T>(workItem);
            }
            else
            {
                throw new Exception();
            }
        }
    }

    class ThreadPoolPerStageExecutionPlan : StagesExecutionPlan
    {
        // public Dictionary<Type, IStageExecutor> currentMapping = new Dictionary<Type, IStageExecutor>();

        public ThreadPoolPerStageExecutionPlan()
        {
            Register<ConcreteStageDefinition>(new ConcreteStageExecutor());
        }

        //  private readonly 
        //  protected override Dictionary<Type, IStageExecutor> currentMapping { get; }
    }

    interface IStageExecutor
    {
        void Execute<T>(Action workItem) where T : IActionDescriptor; // provide default handling? 
    }


    class ConcreteStageDefinition : IStageDefinition
    {
    }

    class ConcreteActionDescription : IActionDescriptor, ActionFaultBehavior.CrashOnFault
    {
    }

    class StagedExecutorService : ExecutorService // rename.? most likely only ExecutorService is to remain
    {
        public StagedExecutorService(NodeConfiguration config)
        {
        }

        public StagedExecutorService(ClientConfiguration config)
        {
        }

        // could be swappable at runtime
        private StagesExecutionPlan currentExecutionPlan = new ThreadPoolPerStageExecutionPlan();


//         class ConcreteStageExecutor : StageExecutor<ConcreteStageDescription>
//        {
//        }

        // overload with func returning promise isn't needed? 

        // returns stageExecutionHandle ? ensure number of concurrent runs constrain. 
        public override void Submit<TStage, T>(Action work)
        {
            currentExecutionPlan.Dispatch<TStage, T>(work);
            throw new NotImplementedException();
        }

        //  ThreadPool.QueueUserWorkItem()
        // this.currentExecutorService.submit
        // current impl: per stage worker pool with optional blocking queue
    }

    class ConcreteStageExecutor : IStageExecutor// - will be abstract
    {
        private Dictionary<Type, LinkedList<IActionWrapper>> workItemWrappers = null;
        // interceptors? currenlty there's no need in multiple wrappers per action  Action[] - will be stack\ likedlist be more descriptive? 
        public ConcreteStageExecutor()
        {
            // precalculate workItem wrapper lambdas?

        }

        public void Execute<T>(Action workItem) where T : IActionDescriptor
        {
           
            if (!workItemWrappers.TryGetValue(typeof(T), out var actionWrappers))
            {
                workItemWrappers[typeof(T)] = actionWrappers = GetActionWrappers<T>();
            }
            //            var qw  = new LinkedList<string>();
            if (actionWrappers.Any())
            {
                var qwe = actionWrappers.First;
                ExecuteActionV2(qwe, workItem);
            }
            else
            {
                workItem();
            }
            throw new NotImplementedException();
        }
       protected interface IActionWrapper
        {
            Type HandlingAttributeType { get; }
            void ExecuteAction(Action action);

        }
        abstract class ActionBehaviorMixin<TActionAttribute> : IActionWrapper where TActionAttribute : IActionAttribute
        {
            public Type HandlingAttributeType { get; } = typeof(TActionAttribute);
            public abstract void ExecuteAction(Action action);
        }

        void ExecuteActionV2(LinkedListNode<IActionWrapper> actionNode, Action action)
        {
            if (actionNode == null)
            {
                action();
                //  workAction();
            }
            else
            {
                ExecuteActionV2(actionNode.Next, action);
            }
        }

        //        ThreadPool.QueueUserWorkItem(state => // ashould be another action wrapper, iotsideschedulerrunnable  behavior
        //                        {
        //                            if (RuntimeContext.Current == null)
        //                            {
        //                                RuntimeContext.Current = new RuntimeContext
        //                                {
        //                                    Scheduler = TaskScheduler.Current
        //    };
        //}
        //agent.Run();
        //                        });

        // todo: describe reasons fornaming
        class OrleansContextRequiredMixin : ActionBehaviorMixin<OrleansContextRequired> // for .net threadpool based executor ( not only? )  // rename
        {
            private TaskScheduler taskScheduler;
            public OrleansContextRequiredMixin(TaskScheduler scheduler)
            {
                taskScheduler = scheduler;
            }

            public override void ExecuteAction(Action action)
            {
                RuntimeContext.InitializeThread(taskScheduler);
                try
                {
                    action();
                }
                finally
                {
                    // deinitialize will be needed for .net threadpool
                }
            }
        }

        class ActionCrashOnFaultBehavior : ActionBehaviorMixin<ActionFaultBehavior.CrashOnFault>
        {
            public override void ExecuteAction(Action action)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    var todo = ex;
                }
            }
        }
        // each stageexecutor - should be able to add its own?
        protected virtual LinkedList<IActionWrapper> GetActionWrappers<T>() where T : IActionDescriptor
        {
            var actionWrappers = new LinkedList<IActionWrapper>();
            var existingWrappersList = new List<IActionWrapper>
            {
                new ActionCrashOnFaultBehavior()
            };

            var tType = typeof(T);
            return existingWrappersList
                .Where(v => v.HandlingAttributeType.IsAssignableFrom(tType))
                .Aggregate(actionWrappers, (list, wrapper) =>
                {
                    list.AddLast(wrapper);
                    return list;
                });
        }
    }

    //  "Enable execution engine config\switch"
    // there should be ability to partially switch stages implementations
    // in order to enable coarce - grained configuration based optimizations

    // AsynchAgent -  long running work. queue agents - many short-running workloads //FaultBehavior

    // move  threading related thing out of AsynchAgent
    // AsynchAgent - becomes more of a trait?, and its content moves into poolThread./ start stop able.
    // take work dispatcher as dependency
    // dispatcher accepts work items + stage definition
    // dispatcher has internal stages to executors mappings (plan)
    // current impl will be mapped to pools of adjusted workerPoolThreads

    // 1 step - ensure ExecuterService resolving in all target places ( AsynchAgent, workerpoolThread) 
    internal abstract class SingleActionAsynchAgent< T> : AsynchAgent
        where T : IActionDescriptor
    {
        // need in empty ctors should be removed in following c# versions https://github.com/dotnet/csharplang/issues/806
        protected SingleActionAsynchAgent(ExecutorService executorService, string nameSuffix, ILoggerFactory loggerFactory) : base(executorService, nameSuffix, loggerFactory)
        {
        }

        protected SingleActionAsynchAgent(ExecutorService executorService, ILoggerFactory loggerFactory) : base(executorService, loggerFactory)
        {
        }

        //        protected override void Run<Q>()
        //            where  Q : IActionDescriptor
        //        {
        //            
        //        }

        // what does start means? - Stage start 

        // for SingleActionAsynchAgent - does it means it should single action? (looks like yes) 
        public override void Start()
        {
//            ApplyPartial((this, "") =>
//            {
//                executorService.SubmitqQ(this, null);
//            })
//            // consider submit typeof(this) as stage
//            executorService.SubmitqQ(this,   null);
            // 'type inference 
            executorService.SubmitW(this, GetAction<T>());
        }
        public static Func<TResult> ApplyPartial<T1, TResult>
            (Func<T1, TResult> function, T1 arg1)
        {
            return () => function(arg1);
        }
    }

    internal abstract class DefaultActionAsynchAgent<TStage> : SingleActionAsynchAgent<TStage, DefaultActionDescriptor>
        where TStage : IStageDefinition
    {
        protected DefaultActionAsynchAgent(ExecutorService executorService, string nameSuffix, ILoggerFactory loggerFactory) : base(executorService, nameSuffix, loggerFactory)
        {
        }

        protected DefaultActionAsynchAgent(ExecutorService executorService, ILoggerFactory loggerFactory) : base(executorService, loggerFactory)
        {
        }
    }

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

    abstract class DefaultActionDescriptor : IActionDescriptor
    {
        private DefaultActionDescriptor()
        {
            
        }

        public void Run()
        {
            throw new NotImplementedException();
        }
    }

    internal abstract class AsynchAgent : IStageDefinition, IDisposable
        //<TStage> :   where TStage : IStageDefinition// , for simplicity for now its not
        // it is actually single stage.. .
        //. Requires implementer to explicitly pass themselves so submit
                                                     // on fault should be passed alongside concrete action 
    {  // asyncg agent- stage reference. 
        protected readonly ExecutorService executorService;
        protected CancellationTokenSource Cts;
        protected object Lockable;
        protected Logger Log;
        private readonly string type;

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

        // there action must be submitted to executor service
        // start of the stage execution.
        public abstract void Start();
        //{
        //    executorService.Submit<>( Run);


            // todo: executor service should ensure concurrent run number guarantees
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

            executorService.Submit(this, GetAction());
        }

        // this is agent business logic
        // run - also action , so will have action descriptor
   //   / protected abstract void Run<T>() where T: IActionDescriptor;
   // todo: verify fit in inheritors
        public abstract T GetAction<T>() where  T: IActionDescriptor;  // { get; } //where T : 
       // void Q<T>()
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
