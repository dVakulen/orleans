using System;
using System.Threading;

namespace Orleans.Runtime.Scheduler
{
    public class Stats
    {
        [ThreadStatic]
        private static Stats context;

        public static Stats Current
        {
            get { return context ?? (context = new Stats()); }
            set { context = value; }
        }

        public Type PreviousT;

        public Type PreviousT2;
        public void setT(Type t)
        {
            PreviousT2 = PreviousT;
            PreviousT = t;
        }
        public long ConsequentlyExecutedBySameThread;
        public int ConsequentlyExecutedBySameThread2;
    }


   
    internal abstract class WorkItemBase : IWorkItem
    {

        internal protected WorkItemBase()
        {
        }

        public ISchedulingContext SchedulingContext { get; set; }
        public TimeSpan TimeSinceQueued 
        {
            get { return Utils.Since(TimeQueued); } 
        }

        public abstract string Name { get; }

        public abstract WorkItemType ItemType { get; }

        public DateTime TimeQueued { get; set; }

        public virtual void Execute() {
          //  Interlocked.Increment(ref OrleansThreadPool.totalExec);
            var curr = Stats.Current;
            var t = GetType();
        //    NameStats.Current.setT(
       //     Name);
         //   ContextStats.Current.setT((SchedulingContext == null) ? "null" : SchedulingContext.Name);
            if (curr.PreviousT == t)
            {
                if (curr.ConsequentlyExecutedBySameThread < 2)
                {
                    curr.ConsequentlyExecutedBySameThread += 1;
                }
                else if (curr.ConsequentlyExecutedBySameThread < 3)
                {

                    curr.ConsequentlyExecutedBySameThread += 2;
                }
                else if (curr.ConsequentlyExecutedBySameThread < 5)
                {

                    curr.ConsequentlyExecutedBySameThread += 3;
                }
                else if (curr.ConsequentlyExecutedBySameThread < 7)
                {

                    curr.ConsequentlyExecutedBySameThread += 4;
                }
                else
                {

                    curr.ConsequentlyExecutedBySameThread += 7;
                }
            }
            else
            {
                Interlocked.Add(ref OrleansThreadPool.consecExec, curr.ConsequentlyExecutedBySameThread);
                curr.ConsequentlyExecutedBySameThread = 0;
                if (curr.PreviousT2 == t)
                {
                  //  curr.ConsequentlyExecutedBySameThread2++;
                }
             
                else
                {
               //     if (curr.ConsequentlyExecutedBySameThread > 0)
                    {
                     //   Interlocked.Add(ref OrleansThreadPool.consecExec2, curr.ConsequentlyExecutedBySameThread2);
                    }
                    curr.ConsequentlyExecutedBySameThread2= 0;
                }
               

                curr.setT(t);
            }

           
        }


        public bool IsSystemPriority
        {
            get { return SchedulingUtils.IsSystemPriorityContext(this.SchedulingContext); }
        }

        public override string ToString()
        {
            return String.Format("[{0} WorkItem Name={1}, Ctx={2}]", 
                ItemType, 
                Name ?? "",
                (SchedulingContext == null) ? "null" : SchedulingContext.ToString()
            );
        }
    }
}

