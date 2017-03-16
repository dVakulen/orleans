using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Runtime
{ /// <summary>Provides a task scheduler that runs tasks on the current thread.</summary>
	public sealed class CurrentThreadTaskScheduler : TaskScheduler
	{
		/// <summary>Runs the provided Task synchronously on the current thread.</summary>
		/// <param name="task">The task to be executed.</param>
		protected override void QueueTask(Task task)
		{
			TryExecuteTask(task);
		}

		/// <summary>Runs the provided Task synchronously on the current thread.</summary>
		/// <param name="task">The task to be executed.</param>
		/// <param name="taskWasPreviouslyQueued">Whether the Task was previously queued to the scheduler.</param>
		/// <returns>True if the Task was successfully executed; otherwise, false.</returns>
		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return TryExecuteTask(task);
		}

		/// <summary>Gets the Tasks currently scheduled to this scheduler.</summary>
		/// <returns>An empty enumerable, as Tasks are never queued, only executed.</returns>
		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return Enumerable.Empty<Task>();
		}

		/// <summary>Gets the maximum degree of parallelism for this scheduler.</summary>
		public override int MaximumConcurrencyLevel { get { return 1; } }
	}
	internal class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
    {
        // Indicates whether the current thread is processing work items.
        [ThreadStatic]
        private static bool _currentThreadIsRunningTasks;

        /// <summary>
        /// Number of tasks currently running
        /// </summary>
        private volatile int _parallelWorkers = 0;

        private readonly LinkedList<Task> _tasks = new LinkedList<Task>();
        public static readonly LimitedConcurrencyLevelTaskScheduler Instance = new LimitedConcurrencyLevelTaskScheduler();

        protected override void QueueTask(Task task)
        {
            lock (_tasks)
            {
                _tasks.AddLast(task);
            }

            EnsureWorkerRequested();
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            //current thread isn't running any tasks, can't execute inline
            if (!_currentThreadIsRunningTasks) return false;

            //remove the task from the queue if it was previously added
            if (taskWasPreviouslyQueued)
                if (TryDequeue(task))
                    return TryExecuteTask(task);
                else
                    return false;
            return TryExecuteTask(task);
        }

        protected override bool TryDequeue(Task task)
        {
            lock (_tasks) return _tasks.Remove(task);
        }

        /// <summary>
        /// Level of concurrency is directly equal to the number of threads
        /// in the <see cref="DedicatedThreadPool"/>.
        /// </summary>
        public override int MaximumConcurrencyLevel
        {
            get { return Environment.ProcessorCount; }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            var lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);

                //should this be immutable?
                if (lockTaken) return _tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasks);
            }
        }

        private void EnsureWorkerRequested()
        {
            var count = _parallelWorkers;
            while (count < Environment.ProcessorCount)
            {
                var prev = Interlocked.CompareExchange(ref _parallelWorkers, count + 1, count);
                if (prev == count)
                {
                    RequestWorker();
                    break;
                }
                count = prev;
            }
        }

        private void ReleaseWorker()
        {
            var count = _parallelWorkers;
            while (count > 0)
            {
                var prev = Interlocked.CompareExchange(ref _parallelWorkers, count - 1, count);
                if (prev == count)
                {
                    break;
                }
                count = prev;
            }
        }

        private void RequestWorker()
        {
            OrleansThreadPool.QueueUserWorkItem((s) =>
            {
                // this thread is now available for inlining
                _currentThreadIsRunningTasks = true;
                try
                {
                    // Process all available items in the queue. 
                    while (true)
                    {
                        Task item;
                        lock (_tasks)
                        {
                            // done processing
                            if (_tasks.Count == 0)
                            {
                                ReleaseWorker();
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        // Execute the task we pulled out of the queue 
                        TryExecuteTask(item);
                    }
                }
                // We're done processing items on the current thread 
                finally { _currentThreadIsRunningTasks = false; }
            });
        }
    }
}
