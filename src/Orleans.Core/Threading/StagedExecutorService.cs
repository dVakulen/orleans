using System.Threading.Tasks;

namespace Orleans.Runtime
{
    class StagedExecutorService : ExecutorService
    {
        private readonly TaskScheduler taskExecutor = new ThreadPerTaskScheduler(task => (task as AsynchAgentTask)?.Name);

        public override void SubmitFromJava(Task work)
        { //StagedExecutorService - actually work dispatcher. concrete executors are schedulers. 
            if (work is AsynchAgentTask)
            {
                // plan.getExecutor
                // actually thread name matters. 
                work.Start(taskExecutor);
                //Is executor service actually TaskScheduler? 
                // fff... no way of adjusting action (is it even needed?)
            }
            else
            {
                work.Start(taskExecutor);
            //    work.Start();
                // submit to shared thread pool
            }
        }

        public override void RunTask(Task task)
        {
            SubmitFromJava(task);
        }
    }
}