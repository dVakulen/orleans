using System.Threading.Tasks;

namespace Orleans.Runtime
{
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
       
        //public override void Submit<TStage>(TStage stage, Action work)
        //{
        //    var z = Task.CompletedTask;
        // //   ThreadPool.QueueUserWorkItem(z);
        //    currentExecutionPlan.Dispatch(stage, work);
        //}

        public override void SubmitFromJava(Task work)
        { //StagedExecutorService - actually work dispatcher. concrete executors are schedulers. 
            if (work is ConcreteStageAction)
            {
                // plan.getExecutor
                work.Start(new ThreadPerTaskScheduler());
                //Is executor service actually TaskScheduler? 
                // fff... no way of adjusting action (is it even needed?)
            }
            else
            {
                work.Start();
                // submit to shared thread pool
            }
        }
    }
}