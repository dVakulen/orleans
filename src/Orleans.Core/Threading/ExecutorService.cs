﻿using System;
using System.Threading.Tasks;
using Orleans.Runtime.Scheduler;

namespace Orleans.Runtime
{
    class ExecutorService : ITaskScheduler
    {
        private readonly TaskScheduler taskExecutor = new ThreadPerTaskScheduler(task => (task as AsynchAgentTask)?.Name);

        public void RunTask(Task task)
        {
            task.Start(taskExecutor);
        }
    }
}