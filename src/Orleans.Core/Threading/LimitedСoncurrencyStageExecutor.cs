using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Threading // could be Concurrency? \ Execution
{
    interface IStageExecutor
    {
        void Execute<T>(T stage, Action workItem) where T : IStageDefinition; // provide default handling? 
    }

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
        }

        private Dictionary<Type, List<StageExecutionInfo>> executingStages 
            = new Dictionary<Type, List<StageExecutionInfo>>();
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
            var q = executingStages[typeof(T)].FirstOrDefault(v => ReferenceEquals(v.Stage, stage));
            // ensure concurrent executions here
            if (!IsStageAvailableForFutherWork(q)) // nullref
            {
                return;
            }

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

        class ActionCrashOnFaultBehavior : ActionBehaviorMixin<ActionFaultBehavior.CrashOnFault>
        {
            public override void ExecuteAction(StageExecutionInfo stage, Action action)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(
                    //    "The {0} agent has thrown an unhandled exception, {1}. The process will be terminated.",
                    //    stage.Stage.Name, exc);
                    //log.Error(ErrorCode.Runtime_Error_100023,
                    //    "AsynchAgent Run method has thrown an unhandled exception. The process will be terminated.",
                    //    exc);
                    //log.Fail(ErrorCode.Runtime_Error_100024, "Terminating process because of an unhandled exception caught in AsynchAgent.Run.");

                    stage.State = StageState.Stopped;// ??
                    var todo = ex;
                }
            }
        }


        // each stageexecutor - should be able to add its own?
        protected virtual LinkedList<IActionWrapper> GetActionWrappers<T>() where T : IStageDefinition
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
}