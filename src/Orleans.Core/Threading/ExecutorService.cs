using System;
using System.Threading.Tasks;
using Orleans.Runtime.Scheduler;

namespace Orleans.Runtime
{
    abstract class ExecutorService : ITaskScheduler // not needed?  .. stage info requirement is leaking from below .. 
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


        //@FunctionalInterface
        //public interface Callable<V>
        //{
        //    /**
        //     * Computes a result, or throws an exception if unable to do so.
        //     *
        //     * @return computed result
        //     * @throws Exception if unable to compute a result
        //     */
        //    V call() throws Exception;
        //}

        // <T> Future<T> submit(Callable<T> task);
        //public abstract void Submit<TStage>(TStage stage, Action work) // TStage,typ
        //    where TStage : IStageDefinition;

        // default task being submitted to shared thread pool
        public abstract void SubmitFromJava(Task work); // TStage,typ
        // where TStage : IStageDefinition;
        
        //       /**
        //* Submits a Runnable task for execution and returns a Future
        //* representing that task. The Future's {@code get} method will
        //* return {@code null} upon <em>successful</em> completion.
        //*
        //* @param task the task to submit
        //* @return a Future representing pending completion of the task
        //* @throws RejectedExecutionException if the task cannot be
        //*         scheduled for execution
        //* @throws NullPointerException if the task is null
        //*/


        // Future<?> submit(Runnable task);



        //public interface Runnable
        //{
        //    /**
        //     * When an object implementing interface <code>Runnable</code> is used
        //     * to create a thread, starting the thread causes the object's
        //     * <code>run</code> method to be called in that separately executing
        //     * thread.
        //     * <p>
        //     * The general contract of the method <code>run</code> is that it may
        //     * take any action whatsoever.
        //     *
        //     * @see     java.lang.Thread#run()
        //     */
        //    public abstract void run();
        //}

        public abstract void RunTask(Task task);
    }
}