using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Runtime;


//    * @since 1.5
//    * @author Doug Lea
//    */
//public interface Executor
//{

//    /**
//     * Executes the given command at some time in the future.  The command
//     * may execute in a new thread, in a pooled thread, or in the calling
//     * thread, at the discretion of the {@code Executor} implementation.
//     *
//     * @param command the runnable task
//     * @throws RejectedExecutionException if this task cannot be
//     * accepted for execution
//     * @throws NullPointerException if command is null
//     */
//    void execute(Runnable command);
//}

namespace Orleans.Threading // could be Concurrency? \ Execution
{
    // to already existing Executor interface ( designed so that .Net ThreadPool could be one of the implementers)
   // 
    interface IStageExecutor
    {
        // should not know about stage, probably? 
        void Execute<T>(T stage, Action workItem) where T : IStageDefinition; // provide default handling? 
        
    }

    // perf stage definition
    class LimitedСoncurrencyStageExecutor : IStageExecutor// - will be abstract
    {

        //        fork-join-executor {
        //# Min number of threads to cap factor-based parallelism number to
        //            parallelism-min = 2
        //# Parallelism (threads) ... ceil(available processors * factor)
        //            parallelism-factor = 2.0
        //# Max number of threads to cap factor-based parallelism number to
        //            parallelism-max = 10
        //        }
        // todo: not threadsafe, change to CachedReadConcurrentDictionary 
        private Dictionary<Type, LinkedList<IActionWrapper>> workItemWrappers = new Dictionary<Type, LinkedList<IActionWrapper>>();
        //
        // Summary:
        //     Specifies the execution states of a System.Threading.Thread.
        [Flags]
        public enum StageState
        {
            Running = 0,
            StopRequested = 1,
            SuspendRequested = 2,
            Suspended= 2,
            Unstarted = 8,
            Stopped = 16
        }
      public  class StageExecutionInfo // rename
        {
            private readonly StageState _state;

            public StageExecutionInfo(IStageDefinition stage)
            {
              //  ThreadPool
                Stage = stage;
                _state = StageState.Unstarted;
            }

            public int CurrentlyExecutingActions { get; set; }
            // todo: add locks
            public IStageDefinition Stage { get; }

            public StageState State
            {
                get { return _state; }
                set { }
            }

            public StageWorkerThread th;
        }
        public class ThreadedStageExecutionInfo: StageExecutionInfo
        {
            public ThreadedStageExecutionInfo(IStageDefinition stage) : base(stage)
            {
            }

        }

        private List<StageExecutionInfo> executingStages 
            = new List<StageExecutionInfo>();
        // interceptors? currenlty there's no need in multiple wrappers per action  Action[] - will be stack\ likedlist be more descriptive? 
        public LimitedСoncurrencyStageExecutor()
        {
            // precalculate workItem wrapper lambdas?

        }

        // todo: should concurrency level be ensured by stage definition, or executors?
        // asynch agent is responcible for stop. f.
        // current eexecution pattern - rare submits of long running work
        public void Execute<T>(T stage, Action workItem) where T : IStageDefinition
        {
            // all stage related activities should be managed in StagedExecutorsService
            var q = executingStages.FirstOrDefault(v => ReferenceEquals(v.Stage, stage));
            // ensure concurrent executions here
            if (!IsStageAvailableForFutherWork(q)) // nullref
            {
                return;
            }
            
            // try start
            if (q.State != StageState.Running)
            {
               // q.th = new StageWorkerThread(stage); //Name = q.Stage.Name 
            }
            if (!workItemWrappers.TryGetValue(typeof(T), out var actionWrappers))
            {
                workItemWrappers[typeof(T)] = actionWrappers = GetActionWrappers<T>();
            }
            //            var qw  = new LinkedList<string>();
            // thread could be started here. with following as lambda
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static void AgentThreadProc(Object obj)
        {
            //var agent = obj as AsynchAgent;
            //if (agent == null)
            //{
            //    throw new InvalidOperationException("Agent thread started with incorrect parameter type");
            //}

            //try
            //{
            //    LogStatus(agent.Log, "Starting AsyncAgent {0} on managed thread {1}", agent.Name, Thread.CurrentThread.ManagedThreadId);
            //    CounterStatistic.SetOrleansManagedThread(); // do it before using CounterStatistic.
            //    CounterStatistic.FindOrCreate(new StatisticName(StatisticNames.RUNTIME_THREADS_ASYNC_AGENT_PERAGENTTYPE, agent.type)).Increment();
            //    CounterStatistic.FindOrCreate(StatisticNames.RUNTIME_THREADS_ASYNC_AGENT_TOTAL_THREADS_CREATED).Increment();
            //    agent.Run();
            //}
            //catch (Exception exc)
            //{
            //    if (agent.State == ThreadState.Running) // If we're stopping, ignore exceptions
            //    {
            //        var log = agent.Log;
            //        switch (agent.OnFault)
            //        {
            //            case FaultBehavior.CrashOnFault:
            //                Console.WriteLine(
            //                    "The {0} agent has thrown an unhandled exception, {1}. The process will be terminated.",
            //                    agent.Name, exc);
            //                log.Error(ErrorCode.Runtime_Error_100023,
            //                    "AsynchAgent Run method has thrown an unhandled exception. The process will be terminated.",
            //                    exc);
            //                log.Fail(ErrorCode.Runtime_Error_100024, "Terminating process because of an unhandled exception caught in AsynchAgent.Run.");
            //                break;
            //            case FaultBehavior.IgnoreFault:
            //                log.Error(ErrorCode.Runtime_Error_100025, "AsynchAgent Run method has thrown an unhandled exception. The agent will exit.",
            //                    exc);
            //                agent.State = ThreadState.Stopped;
            //                break;
            //            case FaultBehavior.RestartOnFault:
            //                log.Error(ErrorCode.Runtime_Error_100026,
            //                    "AsynchAgent Run method has thrown an unhandled exception. The agent will be restarted.",
            //                    exc);
            //                agent.State = ThreadState.Stopped;
            //                try
            //                {
            //                    agent.Start();
            //                }
            //                catch (Exception ex)
            //                {
            //                    log.Error(ErrorCode.Runtime_Error_100027, "Unable to restart AsynchAgent", ex);
            //                    agent.State = ThreadState.Stopped;
            //                }
            //                break;
            //        }
            //    }
            //}
            //finally
            //{
            //    CounterStatistic.FindOrCreate(new StatisticName(StatisticNames.RUNTIME_THREADS_ASYNC_AGENT_PERAGENTTYPE, agent.type)).DecrementBy(1);
            //    agent.Log.Info(ErrorCode.Runtime_Error_100328, "Stopping AsyncAgent {0} that runs on managed thread {1}", agent.Name, Thread.CurrentThread.ManagedThreadId);
            //}
        }

        private bool IsStageAvailableForFutherWork(StageExecutionInfo executionInfo)
        {
            if (executionInfo.Stage is ILimitedСoncurrencyStage limitedСoncurrencyStage)
            {  
//                // LOCK..// todo
//                if (stage.State == StageState.Running)
//                {// probably will be executed in fresh thread,thus leading to unneccessary thread creations
//                    // , but as such submit should be rare event - it shouldnt matter. Or it could be precondition before .. 
//                    return;
//                }
//                if (stage.State == StageState.Stopped)
//                {
//                    return;
//                }
                if (executionInfo.CurrentlyExecutingActions >= limitedСoncurrencyStage.MaximumConcurrencyLevel)
                {
                    return false;
                }
            }
            
            return true;
        }
        protected interface IActionWrapper
        {
            Type HandlingAttributeType { get; }

            void ExecuteAction(StageExecutionInfo stage, Action action);

        }
        // todo: add stage as ExecuteAction parameter
        // add limiting concurrency mixin. StageExecutionInfo should be its holder
        abstract class ActionBehaviorMixin<TActionAttribute> : IActionWrapper where TActionAttribute : IStageAttribute
        {
            public Type HandlingAttributeType { get; } = typeof(TActionAttribute);
            public abstract void ExecuteAction(StageExecutionInfo stage, Action action);
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

            public override void ExecuteAction(StageExecutionInfo stage, Action action)
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
        

        // each stageexecutor - should be able to add its own?
        protected virtual LinkedList<IActionWrapper> GetActionWrappers<T>() where T : IStageDefinition
        {
            var actionWrappers = new LinkedList<IActionWrapper>();
            var existingWrappersList = new List<IActionWrapper>
            {
             ///  new ActionCrashOnFaultBehavior()
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
}