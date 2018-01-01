﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
// todo: dependency on runtime (due to logging)
using Orleans.Runtime;

namespace Orleans.Threading
{
    /// <summary>
    /// Allows clear definition of action behavior wrappers
    /// </summary>
    internal abstract class WorkItemFilter
    {
        private static readonly Action<WorkItemWrapper> NoOpFilter = _ => { };

        public WorkItemFilter(
            Action<WorkItemWrapper> onActionExecuting = null,
            Action<WorkItemWrapper> onActionExecuted = null,
            Func<Exception, WorkItemWrapper, bool> exceptionHandler = null)
            : this(onActionExecuting, onActionExecuted, exceptionHandler, null)
        {
        }

        private WorkItemFilter(
            Action<WorkItemWrapper> onActionExecuting,
            Action<WorkItemWrapper> onActionExecuted,
            Func<Exception, WorkItemWrapper, bool> exceptionHandler,
            WorkItemFilter next)
        {
            Next = next;
            OnActionExecuting = onActionExecuting ?? NoOpFilter;
            OnActionExecuted = onActionExecuted ?? NoOpFilter;
            ExceptionHandler = exceptionHandler ?? ((e, c) => true);
        }

        public WorkItemFilter Next { get; private set; }

        public Func<Exception, WorkItemWrapper, bool> ExceptionHandler { get; }

        public Action<WorkItemWrapper> OnActionExecuting { get; }

        public Action<WorkItemWrapper> OnActionExecuted { get; }

        public bool ExecuteWorkItem(WorkItemWrapper workItem)
        {
            return ExecuteWorkItem(workItem, Next);
        }

        public bool ExecuteWorkItem(WorkItemWrapper workItem, WorkItemFilter next)
        {
            try
            {
                OnActionExecuting(workItem);
                if (next == null)
                {
                    workItem.Execute();
                    return true;
                }
                else
                {
                    return next.ExecuteWorkItem(workItem, next.Next);
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler(ex, workItem))
                {
                    throw;
                }
            }
            finally
            {
                OnActionExecuted(workItem);
            }

            return true;
        }

        public static WorkItemFilter[] CreateChain(IEnumerable<Func<WorkItemFilter>> workItemsFactories)
        {
            WorkItemFilter previousItem = null;
            var workItemFilters = new List<WorkItemFilter>();
            foreach (var fact in workItemsFactories)
            {
                var workItem = fact();
                if (previousItem != null)
                {
                    previousItem.Next = workItem;
                }

                previousItem = workItem;
                workItemFilters.Add(workItem);
            }
            
            return workItemFilters.ToArray();
        }
    }
    
    internal sealed class WorkerThreadStatisticsFilter : WorkItemFilter
    {
        public WorkerThreadStatisticsFilter() : base(
            onActionExecuted: workItem =>
            {
#if TRACK_DETAILED_STATS // todo
                if (todo.ItemType != WorkItemType.WorkItemGroup)
                {
                    if (StatisticsCollector.CollectTurnsStats)
                    {
                        //SchedulerStatisticsGroup.OnTurnExecutionEnd(CurrentStateTime.Elapsed);
                        SchedulerStatisticsGroup.OnTurnExecutionEnd(Utils.Since(CurrentStateStarted));
                    }
                    if (StatisticsCollector.CollectThreadTimeTrackingStats)
                    {
                        threadTracking.IncrementNumberOfProcessed();
                    }
                    CurrentWorkItem = null;
                }
                if (StatisticsCollector.CollectThreadTimeTrackingStats)
                {
                    threadTracking.OnStopProcessing();
                }
#endif
            })
        {
        }
    }
}
