// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Class for creating and managing a threadpool
**
**
=============================================================================*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

#pragma warning disable 0420

/*
 * Below you'll notice two sets of APIs that are separated by the
 * use of 'Unsafe' in their names.  The unsafe versions are called
 * that because they do not propagate the calling stack onto the
 * worker thread.  This allows code to lose the calling stack and 
 * thereby elevate its security privileges.  Note that this operation
 * is much akin to the combined ability to control security policy
 * and control security evidence.  With these privileges, a person 
 * can gain the right to load assemblies that are fully trusted which
 * then assert full trust and can call any code they want regardless
 * of the previous stack information.
 */

namespace Orleans.Runtime
{
    public static class StatsCont
    {
        public class TimeContainer
        {
            public long Time;
        }
        public static ConcurrentDictionary<string, TimeContainer> timestats = new ConcurrentDictionary<string, TimeContainer>();
        public static void TrackNamed(string name, long value)
        {
            TimeContainer v;
            if(!timestats.TryGetValue(name, out v))
            {
                v = new TimeContainer();
                timestats.TryAdd(name,v);
            }

            Interlocked.Add(ref v.Time, value);
        }
        public static ConcurrentQueue<IStats> stats = new ConcurrentQueue<IStats>();
    }
    public interface IStats { }
    public class WaitAndExecTime : IStats
    {
        static WaitAndExecTime()
        {
          //  StatsCont.stats.Enqueue(new WaitAndExecTime());
        }

        public WaitAndExecTime()
        {

            StatsCont.stats.Enqueue(this);
        }
        public override string ToString()
        {
            var q = currentProcessThread.TotalProcessorTime - totalstartUsage;
            var qqq= currentProcessThread.UserProcessorTime - userstartUsage;
            Console.WriteLine("Totals cpu " + q.TotalMilliseconds + " total user cpu " + qqq);
            return base.ToString();
        }
        [ThreadStatic]
        private static WaitAndExecTime context;

        public static WaitAndExecTime Current
        {
            get { return context ?? (context = new WaitAndExecTime()); }
            set { context = value; }
        }

        public Type PreviousT;

        public Type PreviousT2;
        ProcessThread currentProcessThread;
        ProcessThread CurrentProcessThread
        {
            get
            {
                return currentProcessThread ?? (currentProcessThread = OrleansThreadPool.CurrentProcessThread);
            }
        }
        TimeSpan totalstartUsage;
        TimeSpan userstartUsage;
        public void Track()
        {
            totalstartUsage = CurrentProcessThread.TotalProcessorTime;
            userstartUsage = CurrentProcessThread.UserProcessorTime;
        }
    }

    public class WaitExecTimeStats : ExecTimeStats
    {
        private static long elapsed;

        public override long Elapsed => elapsed;


        public override void AddTime(long ms)
        {
            Interlocked.Add(ref elapsed, ms);
        }
        public override string ToString()
        {
            return "Wait" + base.ToString();

        }

        static WaitExecTimeStats()
        {
            StatsCont.stats.Enqueue(new WaitExecTimeStats());
        }
    }
    public abstract class TimeStatsContainer
    {

        public static long elapsed;

        public static string Name;
    }

    public class WorkItemTimeStats : ExecTimeStats
    {
        private static long elapsed;
        TimeStatsContainer cc;
        public override long Elapsed => elapsed;
      

        public override void AddTime(long ms)
        {
            Interlocked.Add(ref elapsed, ms);
        }
        public override string ToString()
        {
            return "Wait" + base.ToString();

        }

        static WorkItemTimeStats()
        {
           // StatsCont.stats.Enqueue(new WorkItemTimeStats());
        }
    }

    public class QueueExecTimeStats : ExecTimeStats
    {
        private static long elapsed;

        public override long Elapsed => elapsed;


        public override void AddTime(long ms)
        {
            Interlocked.Add(ref elapsed, ms);
        }
        public override string ToString()
        {
            return "Queue" + base.ToString();

        }
        static QueueExecTimeStats()
        {
            StatsCont.stats.Enqueue(new QueueExecTimeStats());
        }
    }
    public class EnsureThreadExecTimeStats : ExecTimeStats
    {
        private static long elapsed;

        public override long Elapsed => elapsed;


        public override void AddTime(long ms)
        {
            Interlocked.Add(ref elapsed, ms);
        }
        public override string ToString()
        {
            return "EnsureThread" + base.ToString();

        }
        static EnsureThreadExecTimeStats()
        {
            StatsCont.stats.Enqueue(new EnsureThreadExecTimeStats());
        }
    }

    public class EnsureThreadExecInnerTimeStats : ExecTimeStats
    {
        private static long elapsed;

        public override long Elapsed => elapsed;


        public override void AddTime(long ms)
        {
            Interlocked.Add(ref elapsed, ms);
        }
        public override string ToString()
        {
            return "EnsureThreadExecInnerTimeStats" + base.ToString();

        }
        static EnsureThreadExecInnerTimeStats()
        {
            StatsCont.stats.Enqueue(new EnsureThreadExecInnerTimeStats());
        }
    }
    public class DeQueueExecTimeStats : ExecTimeStats
    {
        private static long elapsed;

        public override long Elapsed => elapsed;


        public override void AddTime(long ms)
        {
            Interlocked.Add(ref elapsed, ms);
        }
        public override string ToString()
        {
            return "DeQueue" + base.ToString();

        }


        static DeQueueExecTimeStats()
        {
            StatsCont.stats.Enqueue(new DeQueueExecTimeStats());
        }
    }

    public abstract class ExecTimeStats : IStats
    {

        public abstract long Elapsed { get; }

        public abstract void AddTime(long ms);
        //protected static void Register()
        //{

        //    StatsCont.stats.Enqueue(Activator.CreateInstance(Ge));
        //}
        //static ExecTimeStats()
        //{
        //    StatsCont.stats.Enqueue(new ExecTimeStats());
        //}
        //public ExecTimeStats()
        //{

        //    StatsCont.stats.Enqueue(this);
        //}
        public override string ToString()
        {
          return " Total ms " + Elapsed;
        }
        //[ThreadStatic]
        //private static WaitAndExecTime context;

        //public static WaitAndExecTime Current
        //{
        //    get { return context ?? (context = new WaitAndExecTime()); }
        //    set { context = value; }
        //}

        private Stopwatch s;
      
        public void Track()
        {
            s = Stopwatch.StartNew();
        }
        public void StopTrack()
        {
          //  AddTime(s.ElapsedMilliseconds);
        }

    }

    public class TimeTracker
    {
        public string Name;
        public TimeTracker(string n)
        {
            Name = n;
        }
        private Stopwatch s = new Stopwatch();
        public TimeTracker Track()
        {
            s.Restart();
            return this;
        }
        public void StopTrack()
        {
      //      StatsCont.TrackNamed(Name, s.ElapsedMilliseconds);
        }
        long localCOunt;
        public void StopTrackLocally()
        {
            localCOunt += s.ElapsedMilliseconds;
        //  StatsCont.TrackNamed(Name, s.ElapsedMilliseconds);
        }
        public void FlushLocalCount()
        {
           

           // StatsCont.TrackNamed(Name, localCOunt);
            localCOunt = 0;

        }
    }

    public class WaitSwitchesStats
    {
        [ThreadStatic]
        private static WaitSwitchesStats context;

        public static WaitSwitchesStats Current
        {
            get { return context ?? (context = new WaitSwitchesStats()); }
            set { context = value; }
        }

        public int PreviousT;

        public int PreviousT2;
        public void setT(int t)
        {
            Interlocked.Increment(ref OrleansThreadPool.waitSwitches);
            return;
            if (PreviousT != t)
            {
                PreviousT = t;
                Interlocked.Increment(ref OrleansThreadPool.waitSwitches);
                return;
                ConsequentlyExecutedBySameThread++;
                if (ConsequentlyExecutedBySameThread < 2)
                {
                    ConsequentlyExecutedBySameThread += 1;
                }
                else if (ConsequentlyExecutedBySameThread < 3)
                {

                    ConsequentlyExecutedBySameThread += 2;
                }
                else if (ConsequentlyExecutedBySameThread < 5)
                {

                    ConsequentlyExecutedBySameThread += 3;
                }
                else if (ConsequentlyExecutedBySameThread < 7)
                {

                    ConsequentlyExecutedBySameThread += 4;
                }
                else
                {

                    ConsequentlyExecutedBySameThread += 7;
                }
            }
            else
            {
                return;
                Interlocked.Add(ref OrleansThreadPool.waitSwitches, ConsequentlyExecutedBySameThread);
                ConsequentlyExecutedBySameThread = 0;
                if (PreviousT2 == t)
                {
                    ConsequentlyExecutedBySameThread2++;
                }
                else
                {
                    //     if (curr.ConsequentlyExecutedBySameThread > 0)
                    {
                        //     Interlocked.Add(ref OrleansThreadPool.stage2, ConsequentlyExecutedBySameThread2);
                    }
                    ConsequentlyExecutedBySameThread2 = 0;
                }

                //  PreviousT2 = PreviousT;
                PreviousT = t;
            }
        }

        public long ConsequentlyExecutedBySameThread;
        public int ConsequentlyExecutedBySameThread2;
    }
    public class SwitchesStats
    {
        [ThreadStatic]
        private static SwitchesStats context;

        public static SwitchesStats Current
        {
            get { return context ?? (context = new SwitchesStats()); }
            set { context = value; }
        }

        public int PreviousT;

        public int PreviousT2;
        public void setT(int t)
        {
            if (PreviousT != t)
            {
                PreviousT = t;
                Interlocked.Increment(ref OrleansThreadPool.workSwitches);
                return;
                ConsequentlyExecutedBySameThread++;
                return;
                if (ConsequentlyExecutedBySameThread < 2)
                {
                    ConsequentlyExecutedBySameThread += 1;
                }
                else if (ConsequentlyExecutedBySameThread < 3)
                {

                    ConsequentlyExecutedBySameThread += 2;
                }
                else if (ConsequentlyExecutedBySameThread < 5)
                {

                    ConsequentlyExecutedBySameThread += 3;
                }
                else if (ConsequentlyExecutedBySameThread < 7)
                {

                    ConsequentlyExecutedBySameThread += 4;
                }
                else
                {

                    ConsequentlyExecutedBySameThread += 7;
                }
            }
            else
            {
                return;
              //  Interlocked.Add(ref OrleansThreadPool.stage1, ConsequentlyExecutedBySameThread);
                ConsequentlyExecutedBySameThread = 0;
                if (PreviousT2 == t)
                {
                    ConsequentlyExecutedBySameThread2++;
                }
                else
                {
                    //     if (curr.ConsequentlyExecutedBySameThread > 0)
                    {
                        //     Interlocked.Add(ref OrleansThreadPool.stage2, ConsequentlyExecutedBySameThread2);
                    }
                    ConsequentlyExecutedBySameThread2 = 0;
                }

                //  PreviousT2 = PreviousT;
                PreviousT = t;
            }
        }

        public long ConsequentlyExecutedBySameThread;
        public int ConsequentlyExecutedBySameThread2;
    }
    internal static class ThreadPoolGlobals
    {
        //Per-appDomain quantum (in ms) for which the thread keeps processing
        //requests in the current domain.
        public const uint TP_QUANTUM = 30U;

        public static readonly int processorCount = Environment.ProcessorCount;


        public static volatile bool vmTpInitialized;
        public static bool enableWorkerTracking;

        public static readonly ThreadPoolWorkQueue workQueue = new ThreadPoolWorkQueue();

        static ThreadPoolGlobals()
        {
        }
    }

    /// <summary>
    /// The type of threads to use - either foreground or background threads.
    /// </summary>
    internal enum ThreadType
    {
        Foreground,
        Background
    }

    internal class SparseArray<T> where T : class
        {
            private volatile T[] m_array;

            internal SparseArray(int initialSize)
            {
                m_array = new T[initialSize];
            }

            internal T[] Current
            {
                get { return m_array; }
            }

            internal int Add(T e)
            {
                while (true)
                {
                    T[] array = m_array;
                    lock (array)
                    {
                        for (int i = 0; i < array.Length; i++)
                        {
                            if (array[i] == null)
                            {
                                Volatile.Write(ref array[i], e);
                                return i;
                            }
                            else if (i == array.Length - 1)
                            {
                                // Must resize. If there was a race condition, we start over again.
                                if (array != m_array)
                                    continue;

                                T[] newArray = new T[array.Length * 2];
                                Array.Copy(array, newArray, i + 1);
                                newArray[i + 1] = e;
                                m_array = newArray;
                                return i + 1;
                            }
                        }
                    }
                }
            }

            internal void Remove(T e)
            {
                T[] array = m_array;
                lock (array)
                {
                    for (int i = 0; i < m_array.Length; i++)
                    {
                        if (m_array[i] == e)
                        {
                            Volatile.Write(ref m_array[i], null);
                            break;
                        }
                    }
                }
            }
        }
    internal sealed class ThreadPoolWorkQueue
    {
        // Simple sparsely populated array to allow lock-free reading.
        

        internal class WorkStealingQueue
        {
            public bool AddingComplete;
            private const int INITIAL_SIZE = 32; // 32
            internal volatile IThreadPoolWorkItem[] m_array = new IThreadPoolWorkItem[INITIAL_SIZE];
            private volatile int m_mask = INITIAL_SIZE - 1;

#if DEBUG
			// in debug builds, start at the end so we exercise the index reset logic.
			private const int START_INDEX = int.MaxValue;
#else
            private const int START_INDEX = 0;
#endif

            private volatile int m_headIndex = START_INDEX;
            private volatile int m_tailIndex = START_INDEX;

            private SpinLock m_foreignLock = new SpinLock(false);

            public void LocalPush(IThreadPoolWorkItem obj)
            {
                int tail = m_tailIndex;

                // We're going to increment the tail; if we'll overflow, then we need to reset our counts
                if (tail == int.MaxValue)
                {
                    bool lockTaken = false;
                    try
                    {
                        m_foreignLock.Enter(ref lockTaken);
                        if (m_tailIndex == int.MaxValue)
                        {
                            //
                            // Rather than resetting to zero, we'll just mask off the bits we don't care about.
                            // This way we don't need to rearrange the items already in the queue; they'll be found
                            // correctly exactly where they are.  One subtlety here is that we need to make sure that
                            // if head is currently < tail, it remains that way.  This happens to just fall out from
                            // the bit-masking, because we only do this if tail == int.MaxValue, meaning that all
                            // bits are set, so all of the bits we're keeping will also be set.  Thus it's impossible
                            // for the head to end up > than the tail, since you can't set any more bits than all of 
                            // them.
                            //
                            m_headIndex = m_headIndex & m_mask;
                            m_tailIndex = tail = m_tailIndex & m_mask;
                            Debug.Assert(m_headIndex <= m_tailIndex);
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                            m_foreignLock.Exit(true);
                    }
                }

                // When there are at least 2 elements' worth of space, we can take the fast path.
                if (tail < m_headIndex + m_mask)
                {
                    Volatile.Write(ref m_array[tail & m_mask], obj);
                    m_tailIndex = tail + 1;
                }
                else
                {
                    // We need to contend with foreign pops, so we lock.
                    bool lockTaken = false;
                    try
                    {
                        m_foreignLock.Enter(ref lockTaken);

                        int head = m_headIndex;
                        int count = m_tailIndex - m_headIndex;

                        // If there is still space (one left), just add the element.
                        if (count >= m_mask)
                        {
                            // We're full; expand the queue by doubling its size.
                            IThreadPoolWorkItem[] newArray = new IThreadPoolWorkItem[m_array.Length << 1];
                            for (int i = 0; i < m_array.Length; i++)
                                newArray[i] = m_array[(i + head) & m_mask];

                            // Reset the field values, incl. the mask.
                            m_array = newArray;
                            m_headIndex = 0;
                            m_tailIndex = tail = count;
                            m_mask = (m_mask << 1) | 1;
                        }

                        Volatile.Write(ref m_array[tail & m_mask], obj);
                        m_tailIndex = tail + 1;
                    }
                    finally
                    {
                        if (lockTaken)
                            m_foreignLock.Exit(false);
                    }
                }
            }

            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
            public bool LocalFindAndPop(IThreadPoolWorkItem obj)
            {
                // Fast path: check the tail. If equal, we can skip the lock.
                if (m_array[(m_tailIndex - 1) & m_mask] == obj)
                {
                    IThreadPoolWorkItem unused;
                    if (LocalPop(out unused))
                    {
                        Debug.Assert(unused == obj);
                        return true;
                    }
                    return false;
                }

                // Else, do an O(N) search for the work item. The theory of work stealing and our
                // inlining logic is that most waits will happen on recently queued work.  And
                // since recently queued work will be close to the tail end (which is where we
                // begin our search), we will likely find it quickly.  In the worst case, we
                // will traverse the whole local queue; this is typically not going to be a
                // problem (although degenerate cases are clearly an issue) because local work
                // queues tend to be somewhat shallow in length, and because if we fail to find
                // the work item, we are about to block anyway (which is very expensive).
                for (int i = m_tailIndex - 2; i >= m_headIndex; i--)
                {
                    if (m_array[i & m_mask] == obj)
                    {
                        // If we found the element, block out steals to avoid interference.
                        bool lockTaken = false;
                        try
                        {
                            m_foreignLock.Enter(ref lockTaken);

                            // If we encountered a race condition, bail.
                            if (m_array[i & m_mask] == null)
                                return false;

                            // Otherwise, null out the element.
                            Volatile.Write(ref m_array[i & m_mask], null);

                            // And then check to see if we can fix up the indexes (if we're at
                            // the edge).  If we can't, we just leave nulls in the array and they'll
                            // get filtered out eventually (but may lead to superflous resizing).
                            if (i == m_tailIndex)
                                m_tailIndex -= 1;
                            else if (i == m_headIndex)
                                m_headIndex += 1;

                            return true;
                        }
                        finally
                        {
                            if (lockTaken)
                                m_foreignLock.Exit(false);
                        }
                    }
                }

                return false;
            }

            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
            public bool LocalPop(out IThreadPoolWorkItem obj)
            {
                while (true)
                {
                    // Decrement the tail using a fence to ensure subsequent read doesn't come before.
                    int tail = m_tailIndex;
                    if (m_headIndex >= tail)
                    {
                        obj = null;
                        return false;
                    }

                    tail -= 1;
                    Interlocked.Exchange(ref m_tailIndex, tail);

                    // If there is no interaction with a take, we can head down the fast path.
                    if (m_headIndex <= tail)
                    {
                        int idx = tail & m_mask;
                        obj = Volatile.Read(ref m_array[idx]);

                        // Check for nulls in the array.
                        if (obj == null) continue;

                        m_array[idx] = null;
                        return true;
                    }
                    else
                    {
                        // Interaction with takes: 0 or 1 elements left.
                        bool lockTaken = false;
                        try
                        {
                            m_foreignLock.Enter(ref lockTaken);

                            if (m_headIndex <= tail)
                            {
                                // Element still available. Take it.
                                int idx = tail & m_mask;
                                obj = Volatile.Read(ref m_array[idx]);

                                // Check for nulls in the array.
                                if (obj == null) continue;

                                m_array[idx] = null;
                                return true;
                            }
                            else
                            {
                                // If we encountered a race condition and element was stolen, restore the tail.
                                m_tailIndex = tail + 1;
                                obj = null;
                                return false;
                            }
                        }
                        finally
                        {
                            if (lockTaken)
                                m_foreignLock.Exit(false);
                        }
                    }
                }
            }

            public bool TrySteal(out IThreadPoolWorkItem obj, ref bool missedSteal)
            {
                return TrySteal(out obj, ref missedSteal, 0); // no blocking by default.
            }

            private bool TrySteal(out IThreadPoolWorkItem obj, ref bool missedSteal, int millisecondsTimeout)
            {
                obj = null;

                while (true)
                {
                    if (m_headIndex >= m_tailIndex)
                        return false;

                    bool taken = false;
                    try
                    {
                        m_foreignLock.TryEnter(millisecondsTimeout, ref taken);
                        if (taken)
                        {
                            // Increment head, and ensure read of tail doesn't move before it (fence).
                            int head = m_headIndex;
                            Interlocked.Exchange(ref m_headIndex, head + 1);

                            if (head < m_tailIndex)
                            {
                                int idx = head & m_mask;
                                obj = Volatile.Read(ref m_array[idx]);

                                // Check for nulls in the array.
                                if (obj == null) continue;

                                m_array[idx] = null;
                                return true;
                            }
                            else
                            {
                                // Failed, restore head.
                                m_headIndex = head;
                                obj = null;
                                missedSteal = true;
                            }
                        }
                        else
                        {
                            missedSteal = true;
                        }
                    }
                    finally
                    {
                        if (taken)
                            m_foreignLock.Exit(false);
                    }

                    return false;
                }
            }
        }

        internal class QueueSegment
        {
            // Holds a segment of the queue.  Enqueues/Dequeues start at element 0, and work their way up.
            internal readonly IThreadPoolWorkItem[] nodes;
            private const int QueueSegmentLength = 256;

            // Holds the indexes of the lowest and highest valid elements of the nodes array.
            // The low index is in the lower 16 bits, high index is in the upper 16 bits.
            // Use GetIndexes and CompareExchangeIndexes to manipulate this.
            private volatile int indexes;

            // The next segment in the queue.
            public volatile QueueSegment Next;


            const int SixteenBits = 0xffff;

            void GetIndexes(out int upper, out int lower)
            {
                int i = indexes;
                upper = (i >> 16) & SixteenBits;
                lower = i & SixteenBits;

                Debug.Assert(upper >= lower);
                Debug.Assert(upper <= nodes.Length);
                Debug.Assert(lower <= nodes.Length);
                Debug.Assert(upper >= 0);
                Debug.Assert(lower >= 0);
            }

            bool CompareExchangeIndexes(ref int prevUpper, int newUpper, ref int prevLower, int newLower)
            {
                Debug.Assert(newUpper >= newLower);
                Debug.Assert(newUpper <= nodes.Length);
                Debug.Assert(newLower <= nodes.Length);
                Debug.Assert(newUpper >= 0);
                Debug.Assert(newLower >= 0);
                Debug.Assert(newUpper >= prevUpper);
                Debug.Assert(newLower >= prevLower);
                Debug.Assert(newUpper == prevUpper ^ newLower == prevLower);

                int oldIndexes = (prevUpper << 16) | (prevLower & SixteenBits);
                int newIndexes = (newUpper << 16) | (newLower & SixteenBits);
                int prevIndexes = Interlocked.CompareExchange(ref indexes, newIndexes, oldIndexes);
                prevUpper = (prevIndexes >> 16) & SixteenBits;
                prevLower = prevIndexes & SixteenBits;
                return prevIndexes == oldIndexes;
            }

            public QueueSegment()
            {
                Debug.Assert(QueueSegmentLength <= SixteenBits);
                nodes = new IThreadPoolWorkItem[QueueSegmentLength];
            }


            public bool IsUsedUp()
            {
                int upper, lower;
                GetIndexes(out upper, out lower);
                return (upper == nodes.Length) &&
                       (lower == nodes.Length);
            }

            public bool TryEnqueue(IThreadPoolWorkItem node)
            {
                //
                // If there's room in this segment, atomically increment the upper count (to reserve
                // space for this node), then store the node.
                // Note that this leaves a window where it will look like there is data in that
                // array slot, but it hasn't been written yet.  This is taken care of in TryDequeue
                // with a busy-wait loop, waiting for the element to become non-null.  This implies
                // that we can never store null nodes in this data structure.
                //
                Debug.Assert(null != node);

                int upper, lower;
                GetIndexes(out upper, out lower);

                while (true)
                {
                    if (upper == nodes.Length)
                        return false;

                    if (CompareExchangeIndexes(ref upper, upper + 1, ref lower, lower))
                    {
                        Debug.Assert(Volatile.Read(ref nodes[upper]) == null);
                        Volatile.Write(ref nodes[upper], node);
                        return true;
                    }
                }
            }

            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
            public bool TryDequeue(out IThreadPoolWorkItem node)
            {
                //
                // If there are nodes in this segment, increment the lower count, then take the
                // element we find there.
                //
                int upper, lower;
                GetIndexes(out upper, out lower);

                while (true)
                {
                    if (lower == upper)
                    {
                        node = null;
                        return false;
                    }

                    if (CompareExchangeIndexes(ref upper, upper, ref lower, lower + 1))
                    {
                        // It's possible that a concurrent call to Enqueue hasn't yet
                        // written the node reference to the array.  We need to spin until
                        // it shows up.
                        SpinWait spinner = new SpinWait();
                        while ((node = Volatile.Read(ref nodes[lower])) == null)
                            spinner.SpinOnce();

                        // Null-out the reference so the object can be GC'd earlier.
                        nodes[lower] = null;

                        return true;
                    }
                }
            }
        }

        // The head and tail of the queue.  We enqueue to the head, and dequeue from the tail.
        internal volatile QueueSegment queueHead;
        internal volatile QueueSegment queueTail;
        internal bool loggingEnabled;

        internal static readonly SparseArray<WorkStealingQueue> allThreadQueues = new SparseArray<WorkStealingQueue>(16);

        private volatile int numOutstandingThreadRequests = 0;

        public ThreadPoolWorkQueue()
        {
            queueTail = queueHead = new QueueSegment();
        }

        public ThreadPoolWorkQueueThreadLocals EnsureCurrentThreadHasQueue()
        {
            if (null == ThreadPoolWorkQueueThreadLocals.threadLocals)
                ThreadPoolWorkQueueThreadLocals.threadLocals = new ThreadPoolWorkQueueThreadLocals(this);
            return ThreadPoolWorkQueueThreadLocals.threadLocals;
        }

        internal void EnsureThreadRequested()
        {
            //
            // If we have not yet requested #procs threads from the VM, then request a new thread.
            // Note that there is a separate count in the VM which will also be incremented in this case, 
            // which is handled by RequestWorkerThread.
            //
            var b = new EnsureThreadExecTimeStats();
            b.Track();
           // Interlocked.
            Interlocked.Increment(ref OrleansThreadPool.outerRepeats);
            int count = numOutstandingThreadRequests;
            while (count < ThreadPoolGlobals.processorCount)
            {
                var qwb = new EnsureThreadExecInnerTimeStats();
                qwb.Track();
                Interlocked.Increment(ref OrleansThreadPool.innerRepeats);
                int prev = Interlocked.CompareExchange(ref numOutstandingThreadRequests, count + 1, count);
                if (prev == count)
                {
                    OrleansThreadPool.RequestWorkerThread();
                    qwb.StopTrack();
                    break;
                }
                count = prev;
                qwb.StopTrack();
            }
            b.StopTrack();
        }


        internal void MarkThreadRequestSatisfied()
        {
            //
            // The VM has called us, so one of our outstanding thread requests has been satisfied.
            // Decrement the count so that future calls to EnsureThreadRequested will succeed.
            // Note that there is a separate count in the VM which has already been decremented by the VM
            // by the time we reach this point.
            //

            Interlocked.Increment(ref OrleansThreadPool.outerMarkRepeats);
            int count = numOutstandingThreadRequests;
            while (count > 0)
            {
                Interlocked.Increment(ref OrleansThreadPool.innerMarkRepeats);
                int prev = Interlocked.CompareExchange(ref numOutstandingThreadRequests, count - 1, count);
                if (prev == count)
                {
                    break;
                }
                count = prev;
            }
        }

        public void Enqueue(IThreadPoolWorkItem callback, bool forceGlobal)
        {
            ThreadPoolWorkQueueThreadLocals tl = null;
            if (!forceGlobal)
                tl = ThreadPoolWorkQueueThreadLocals.threadLocals;


            if (null != tl)
            {
                tl.workStealingQueue.LocalPush(callback);
            }
            else
            {
                QueueSegment head = queueHead;

                while (!head.TryEnqueue(callback))
                {
                    Interlocked.CompareExchange(ref head.Next, new QueueSegment(), null);

                    while (head.Next != null)
                    {
                        Interlocked.CompareExchange(ref queueHead, head.Next, head);
                        head = queueHead;
                    }
                }
            }

            EnsureThreadRequested();
        }

        internal bool LocalFindAndPop(IThreadPoolWorkItem callback)
        {
            ThreadPoolWorkQueueThreadLocals tl = ThreadPoolWorkQueueThreadLocals.threadLocals;
            if (null == tl)
                return false;

            return tl.workStealingQueue.LocalFindAndPop(callback);
        }

        public void Dequeue(ThreadPoolWorkQueueThreadLocals tl, out IThreadPoolWorkItem callback, out bool missedSteal)
        {
            callback = null;
            missedSteal = false;
            WorkStealingQueue wsq = tl.workStealingQueue;

            wsq.LocalPop(out callback);

            if (null == callback)
            {
                QueueSegment tail = queueTail;
                while (true)
                {
                    if (tail.TryDequeue(out callback))
                    {
                        Debug.Assert(null != callback);
                        break;
                    }

                    if (null == tail.Next || !tail.IsUsedUp())
                    {
                        break;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref queueTail, tail.Next, tail);
                        tail = queueTail;
                    }
                }
            }

            if (null == callback)
            {
                WorkStealingQueue[] otherQueues = allThreadQueues.Current;
                int c = otherQueues.Length;
                int maxIndex = c - 1;
                int i = tl.random.Next(c);
                while (c > 0)
                {
                    i = (i < maxIndex) ? i + 1 : 0;
                    WorkStealingQueue otherQueue = Volatile.Read(ref otherQueues[i]);
                    if (otherQueue != null &&
                        otherQueue != wsq &&
                        otherQueue.TrySteal(out callback, ref missedSteal))
                    {
                        Debug.Assert(null != callback);
                        break;
                    }
                    c--;
                }
            }
        }

        static internal bool Dispatch()
        {
            var workQueue = ThreadPoolGlobals.workQueue;


            var bqwe = new TimeTracker("DispatchInner");

            var DispatchV2 = new TimeTracker("DispatchV2").Track();
            var DispatchStage0 = new TimeTracker("DispatchStage0");
            var DispatchStage1 = new TimeTracker("DispatchStage1");
            var DispatchStage2 = new TimeTracker("DispatchStage2");
            var DispatchStage3 = new TimeTracker("DispatchStage3");
            var DispatchInnerV2 = new TimeTracker("DispatchInnerV2").Track();
            var DequeueStat = new TimeTracker("DequeueStat");
            //
            // Update our records to indicate that an outstanding request for a thread has now been fulfilled.
            // From this point on, we are responsible for requesting another thread if we stop working for any
            // reason, and we believe there might still be work in the queue.
            //
            // Note that if this thread is aborted before we get a chance to request another one, the VM will
            // record a thread request on our behalf.  So we don't need to worry about getting aborted right here.
            //
            DispatchStage0.Track();
            workQueue.MarkThreadRequestSatisfied();
            DispatchStage0.StopTrack();
            //
            // Assume that we're going to need another thread if this one returns to the VM.  We'll set this to 
            // false later, but only if we're absolutely certain that the queue is empty.
            //
            bool needAnotherThread = true;
            IThreadPoolWorkItem workItem = null;
            try
            {
                // Set up our thread-local data
                //
                //var MarkThreadRequestSatisfied = new TimeTracker("EnsureCurrentThreadHasQueue").Track();
                DispatchStage1.Track();
                ThreadPoolWorkQueueThreadLocals tl = workQueue.EnsureCurrentThreadHasQueue();
              //  MarkThreadRequestSatisfied.StopTrack();
               // if (tl.workStealingQueue.AddingComplete)
                {
                 //   return false;
                }
                DispatchStage1.StopTrack();
                //
                // Loop until our quantum expires.
                try { }
                finally
                {
                    while (true)
                    {
                        DispatchStage2.Track();
                        DispatchInnerV2.Track();
                        //
                        //
                        // Dequeue and EnsureThreadRequested must be protected from ThreadAbortException.  
                        // These are fast, so this will not delay aborts/AD-unloads for very long.
                        //
                        try { }
                        finally
                        {
                            bool missedSteal = false;

                            DequeueStat.Track();
                            workQueue.Dequeue(tl, out workItem, out missedSteal);
                            DequeueStat.StopTrackLocally();
                            if (workItem == null)
                            {
                                //
                                // No work.  We're going to return to the VM once we leave this protected region.
                                // If we missed a steal, though, there may be more work in the queue.
                                // Instead of looping around and trying again, we'll just request another thread.  This way
                                // we won't starve other AppDomains while we spin trying to get locks, and hopefully the thread
                                // that owns the contended work-stealing queue will pick up its own workitems in the meantime, 
                                // which will be more efficient than this thread doing it anyway.
                                //
                                needAnotherThread = missedSteal;
                            }
                            else
                            {
                                //
                                // If we found work, there may be more work.  Ask for another thread so that the other work can be processed
                                // in parallel.  Note that this will only ask for a max of #procs threads, so it's safe to call it for every dequeue.
                                // // todo: for now this isn't needed, as threads arent being requested from VM
                                // workQueue.EnsureThreadRequested();
                            }
                        }
                        DispatchStage2.StopTrackLocally();
                        DispatchStage3.Track();
                        if (workItem == null)
                        {
                            // Tell the VM we're returning normally, not because Hill Climbing asked us to return.
                            break;
                        }
                        else
                        {
                            //
                            // Execute the workitem outside of any finally blocks, so that it can be aborted if needed.
                            //
                            Interlocked.Increment(ref OrleansThreadPool.ExecutedItems);

                            bqwe.Track();
                            SwitchesStats.Current.setT((int)OrleansThreadPool.GetCurrentProcessorNumber());
                            workItem.ExecuteWorkItem();
                            SwitchesStats.Current.setT(OrleansThreadPool.GetCurrentProcessorNumber());
                            bqwe.StopTrackLocally();

                            workItem = null;

                            // 
                            // Notify the VM that we executed this workitem.  This is also our opportunity to ask whether Hill Climbing wants
                            // us to return the thread to the pool or not.
                            // todo : for hill climbing 
                            //if (!ThreadPool.NotifyWorkItemComplete())
                            //	return false;
                        }
                        DispatchStage3.StopTrackLocally();
                        DispatchInnerV2.StopTrackLocally();
                    }
                }


            }

            finally
            {
                DispatchStage2.FlushLocalCount();
                DispatchStage3.FlushLocalCount();
                DispatchInnerV2.FlushLocalCount();
                DispatchV2.StopTrack();
                bqwe.FlushLocalCount();
                DequeueStat.FlushLocalCount();
                //
                // If we are exiting for any reason other than that the queue is definitely empty, ask for another
                // thread to pick up where we left off.
                //
                if (needAnotherThread)
                    workQueue.EnsureThreadRequested();
            }

            // we can never reach this point, but the C# compiler doesn't know that, because it doesn't know the ThreadAbortException will be reraised above.
         //   Debug.Assert(false);
            return true;
        }

    }

    // Holds a WorkStealingQueue, and remmoves it from the list when this object is no longer referened.
    internal sealed class ThreadPoolWorkQueueThreadLocals
    {
        [ThreadStatic]
        public static ThreadPoolWorkQueueThreadLocals threadLocals;

        public readonly ThreadPoolWorkQueue workQueue;
        public readonly ThreadPoolWorkQueue.WorkStealingQueue workStealingQueue;
        public readonly Random random = new Random(Thread.CurrentThread.ManagedThreadId);

        public ThreadPoolWorkQueueThreadLocals(ThreadPoolWorkQueue tpq)
        {
            workQueue = tpq;
            workStealingQueue = new ThreadPoolWorkQueue.WorkStealingQueue();
            ThreadPoolWorkQueue.allThreadQueues.Add(workStealingQueue);
        }

        private void CleanUp()
        {
            if (null != workStealingQueue)
            {
                if (null != workQueue)
                {
                    bool done = false;
                    while (!done)
                    {
                        // Ensure that we won't be aborted between LocalPop and Enqueue.
                        try { }
                        finally
                        {
                            IThreadPoolWorkItem cb = null;
                            if (workStealingQueue.LocalPop(out cb))
                            {
                                Debug.Assert(null != cb);
                                workQueue.Enqueue(cb, true);
                            }
                            else
                            {
                                done = true;
                            }
                        }
                    }
                }

                ThreadPoolWorkQueue.allThreadQueues.Remove(workStealingQueue);
            }
        }

        ~ThreadPoolWorkQueueThreadLocals()
        {
            // Since the purpose of calling CleanUp is to transfer any pending workitems into the global
            // queue so that they will be executed by another thread, there's no point in doing this cleanup
            // if we're in the process of shutting down or unloading the AD.  In those cases, the work won't
            // execute anyway.  And there are subtle race conditions involved there that would lead us to do the wrong
            // thing anyway.  So we'll only clean up if this is a "normal" finalization.
            if (!(Environment.HasShutdownStarted))
                CleanUp();
        }
    }


    //[System.Runtime.InteropServices.ComVisible(true)]
    //public delegate void WaitCallback(Object state);


    //
    // Interface to something that can be queued to the TP.  This is implemented by 
    // QueueUserWorkItemCallback, Task, and potentially other internal types.
    // For example, SemaphoreSlim represents callbacks using its own type that
    // implements IThreadPoolWorkItem.
    //
    // If we decide to expose some of the workstealing
    // stuff, this is NOT the thing we want to expose to the public.
    //
    internal interface IThreadPoolWorkItem
    {
        void ExecuteWorkItem();
        void MarkAborted(
#if !NETSTANDARD
            ThreadAbortException tae
#endif
            );
    }

    internal sealed class QueueUserWorkItemCallback : IThreadPoolWorkItem
    {
        static QueueUserWorkItemCallback() { }

        private WaitCallback callback;
        private ExecutionContext context;
        private Object state;

#if DEBUG
		volatile int executed;

		~QueueUserWorkItemCallback()
		{
			Debug.Assert(
				executed != 0 || Environment.HasShutdownStarted,
				"A QueueUserWorkItemCallback was never called!");
		}

		void MarkExecuted(bool aborted)
		{
			GC.SuppressFinalize(this);
			Debug.Assert(
				0 == Interlocked.Exchange(ref executed, 1) || aborted,
				"A QueueUserWorkItemCallback was called twice!");
		}
#endif

        internal QueueUserWorkItemCallback(WaitCallback waitCallback, Object stateObj, ExecutionContext ec)
        {
            callback = waitCallback;
            state = stateObj;
            context = ec;
        }

        void IThreadPoolWorkItem.ExecuteWorkItem()
        {
#if DEBUG
			MarkExecuted(false);
#endif
            // call directly if it is an unsafe call OR EC flow is suppressed
            if (context == null)
            {
                WaitCallback cb = callback;
                callback = null;
                cb(state);
            }
            else
            {
                ExecutionContext.Run(context, ccb, this);
            }
        }

        void IThreadPoolWorkItem.MarkAborted(
#if !NETSTANDARD
            ThreadAbortException tae
#endif
            )
        {
#if DEBUG
			// this workitem didn't execute because we got a ThreadAbortException prior to the call to ExecuteWorkItem.  
			// This counts as being executed for our purposes.
			MarkExecuted(true);
#endif
        }

        static internal ContextCallback ccb = new ContextCallback(WaitCallback_Context);

        static private void WaitCallback_Context(Object state)
        {
            QueueUserWorkItemCallback obj = (QueueUserWorkItemCallback)state;
            WaitCallback wc = obj.callback as WaitCallback;
            Debug.Assert(null != wc);
            wc(obj.state);
        }
    }

    internal sealed class QueueUserWorkItemCallbackDefaultContext : IThreadPoolWorkItem
    {
        static QueueUserWorkItemCallbackDefaultContext() { }

        private readonly WaitCallback callback;
        private readonly Object state;

#if DEBUG
		private volatile int executed;

		~QueueUserWorkItemCallbackDefaultContext()
		{
			Debug.Assert(
				executed != 0 || Environment.HasShutdownStarted,
				"A QueueUserWorkItemCallbackDefaultContext was never called!");
		}

		void MarkExecuted(bool aborted)
		{
			GC.SuppressFinalize(this);
			Debug.Assert(
				0 == Interlocked.Exchange(ref executed, 1) || aborted,
				"A QueueUserWorkItemCallbackDefaultContext was called twice!");
		}
#endif

        internal QueueUserWorkItemCallbackDefaultContext(WaitCallback waitCallback, Object stateObj)
        {
            callback = waitCallback;
            state = stateObj;
        }

        void IThreadPoolWorkItem.ExecuteWorkItem()
        {
#if DEBUG
			MarkExecuted(false);
#endif
            callback(state);
        }

        void IThreadPoolWorkItem.MarkAborted(
#if !NETSTANDARD
            ThreadAbortException tae
#endif
            )
        {
#if DEBUG
			// this workitem didn't execute because we got a ThreadAbortException prior to the call to ExecuteWorkItem.  
			// This counts as being executed for our purposes.
			MarkExecuted(true);
#endif
        }

        //static internal ContextCallback ccb = new ContextCallback(WaitCallback_Context);

        //static private void WaitCallback_Context(Object state)
        //{
        //	QueueUserWorkItemCallbackDefaultContext obj = (QueueUserWorkItemCallbackDefaultContext)state;
        //	WaitCallback wc = obj.callback as WaitCallback;
        //	Debug.Assert(null != wc);
        //	obj.callback = null;
        //	wc(obj.state);
        //}
    }

    public static class OrleansThreadPool
    {
        public static long waitSwitches;
        public static long ExecutedItems;
        public static long workSwitches;
        private static readonly List<Thread> _workerThreads = new List<Thread>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QueueUserWorkItem(WaitCallback callBack, Object state)
        {

            return QueueUserWorkItemHelper(callBack, state);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QueueSystemWorkItem(WaitCallback callBack, Object state)
        {

            return QueueUserWorkItemHelper(callBack, state);
        }

        private static int gg;

        private static readonly UnfairSemaphore _semaphore = new UnfairSemaphore();

        public static void NotifyWorkItemComplete()
        {
            Interlocked.Decrement(ref gg);
        }
        public static long innerRepeats;

        public static long innerMarkRepeats;
        public static long outerMarkRepeats;
        public static long outerRepeats;
        public static void RequestWorkerThread()
        {
            _semaphore.Release();
        }

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll")]
        public static extern int GetCurrentProcessorNumber();
        private static void SetThreadAffinity(int processorIndex)
        {
            // notify the runtime we are going to use affinity
            Thread.BeginThreadAffinity();

            // we can now safely access the corresponding native thread
            var processThread = CurrentProcessThread;

            var affinity = (1 << processorIndex);
           // affinity = processorIndex;
            processThread.ProcessorAffinity = new IntPtr(affinity);
           // processThread.IdealProcessor = affinity;// new IntPtr(affinity);
        }

        public static ProcessThread CurrentProcessThread
        {
            get
            {
                var threadId = GetCurrentThreadId();

                foreach (ProcessThread processThread in Process.GetCurrentProcess().Threads)
                {
                    if (processThread.Id == threadId)
                    {
                        return processThread;
                    }
                }

                throw new InvalidOperationException(
                    string.Format("Could not retrieve native thread with ID: {0}, current managed thread ID was {1}",
                                  threadId, Thread.CurrentThread.ManagedThreadId));
            }
        }

        private static void RemoveThreadAffinity()
        {
            var processThread = CurrentProcessThread;

            var affinity = (1 << Environment.ProcessorCount) - 1;
            // processThread.
            processThread.ProcessorAffinity = new IntPtr(affinity);

            Thread.EndThreadAffinity();
        }

        static OrleansThreadPool()
        {
            _workerThreads.AddRange(Enumerable.Range(0, Environment.ProcessorCount * 2)
                .Select(v => new Thread(() =>
                {
                    //SetThreadAffinity(v);
                   // new WaitAndExecTime().Track();
                    while (true)
                    {
                        try
                        {
                            var b = new WaitExecTimeStats();
                            b.Track();
                              _semaphore.Wait();
                            b.StopTrack();
                            var ew = new TimeTracker("Dispatch").Track();
                            //WaitSwitchesStats.Current.setT((int)GetCurrentProcessorNumber());
                            if (!ThreadPoolWorkQueue.Dispatch())
                            {
                                ew.StopTrack();
                                // todo: need to drain local queue into global one if environment still running
                                return;
                            }
                            ew.StopTrack();
                            SwitchesStats.Current.setT((int)GetCurrentProcessorNumber());
                        }
                        catch (Exception ex)
#if !NETSTANDARD
                        when (!(ex is ThreadAbortException))
#endif
                        {
                            // todo: normalize logging

                            LogManager.GetLogger("OrleansThreadPool").Log(0, Severity.Error, "", null, ex);
                       }
                    }
                })).ToList());

            for (var i = 0; i < _workerThreads.Count; i++)
            {
                _workerThreads[i].Name = $"OrleansThreadPoolThread_{i.ToString()}";
                _workerThreads[i].IsBackground = true;
                _workerThreads[i].Priority = ThreadPriority.Highest; 
            }

            _workerThreads.ForEach(v => v.Start());
#if !NETSTANDARD
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
#endif

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            try
            {
                foreach (var tl in ThreadPoolWorkQueue.allThreadQueues.Current)
                {
                    if (tl != null)
                    {
                        tl.AddingComplete = true;
                    }
                }
            }
            catch (Exception exc)
            {
                // ignore. Just make sure DomainUnload handler does not throw.
                //Log.Verbose("Ignoring error during Stop: {0}", exc);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QueueUserWorkItem(WaitCallback callBack)
        {

            return QueueUserWorkItemHelper(callBack, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QueueSystemWorkItem(WaitCallback callBack)
        {

            return QueueUserWorkItemHelper(callBack, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool QueueUserWorkItemHelper(WaitCallback callBack, Object state)
        {
          //  return ThreadPool.UnsafeQueueUserWorkItem(callBack, state);
            //if (!Thread.CurrentThread.IsThreadPoolThread)
            //{
            //    if (RuntimeContext.Current == null)
            //    {
            //        RuntimeContext.Current = new RuntimeContext
            //        {
            //            Scheduler = new O
            //        };
            //    }
            //    callBack(state);
            //    return true;
            //}
            bool success = false;
            var b =  new QueueExecTimeStats();
            b.Track();
            //
            // If we are able to create the workitem, we need to get it in the queue without being interrupted
            // by a ThreadAbortException.
            //
            try { }
            finally
            {
                IThreadPoolWorkItem tpcallBack = new QueueUserWorkItemCallbackDefaultContext(callBack, state);

                ThreadPoolGlobals.workQueue.Enqueue(tpcallBack, false);
                success = true;
            }

            b.StopTrack();
            return success;
        }

        // This method tries to take the target callback out of the current thread's queue.
        internal static bool TryPopCustomWorkItem(IThreadPoolWorkItem workItem)
        {
            Debug.Assert(null != workItem);
            if (!ThreadPoolGlobals.vmTpInitialized)
                return false; //Not initialized, so there's no way this workitem was ever queued.
            return ThreadPoolGlobals.workQueue.LocalFindAndPop(workItem);
        }

        // Get all workitems.  Called by TaskScheduler in its debugger hooks.
        internal static IEnumerable<IThreadPoolWorkItem> GetQueuedWorkItems()
        {
            return EnumerateQueuedWorkItems(ThreadPoolWorkQueue.allThreadQueues.Current, ThreadPoolGlobals.workQueue.queueTail);
        }

        internal static IEnumerable<IThreadPoolWorkItem> EnumerateQueuedWorkItems(ThreadPoolWorkQueue.WorkStealingQueue[] wsQueues, ThreadPoolWorkQueue.QueueSegment globalQueueTail)
        {
            if (wsQueues != null)
            {
                // First, enumerate all workitems in thread-local queues.
                foreach (ThreadPoolWorkQueue.WorkStealingQueue wsq in wsQueues)
                {
                    if (wsq != null && wsq.m_array != null)
                    {
                        IThreadPoolWorkItem[] items = wsq.m_array;
                        for (int i = 0; i < items.Length; i++)
                        {
                            IThreadPoolWorkItem item = items[i];
                            if (item != null)
                                yield return item;
                        }
                    }
                }
            }

            if (globalQueueTail != null)
            {
                // Now the global queue
                for (ThreadPoolWorkQueue.QueueSegment segment = globalQueueTail;
                    segment != null;
                    segment = segment.Next)
                {
                    IThreadPoolWorkItem[] items = segment.nodes;
                    for (int i = 0; i < items.Length; i++)
                    {
                        IThreadPoolWorkItem item = items[i];
                        if (item != null)
                            yield return item;
                    }
                }
            }
        }

        internal static IEnumerable<IThreadPoolWorkItem> GetLocallyQueuedWorkItems()
        {
            return EnumerateQueuedWorkItems(new ThreadPoolWorkQueue.WorkStealingQueue[] { ThreadPoolWorkQueueThreadLocals.threadLocals.workStealingQueue }, null);
        }

        internal static IEnumerable<IThreadPoolWorkItem> GetGloballyQueuedWorkItems()
        {
            return EnumerateQueuedWorkItems(null, ThreadPoolGlobals.workQueue.queueTail);
        }

        private static object[] ToObjectArray(IEnumerable<IThreadPoolWorkItem> workitems)
        {
            int i = 0;
            foreach (IThreadPoolWorkItem item in workitems)
            {
                i++;
            }

            object[] result = new object[i];
            i = 0;
            foreach (IThreadPoolWorkItem item in workitems)
            {
                if (i < result.Length) //just in case someone calls us while the queues are in motion
                    result[i] = item;
                i++;
            }

            return result;
        }

        // This is the method the debugger will actually call, if it ends up calling
        // into ThreadPool directly.  Tests can use this to simulate a debugger, as well.
        internal static object[] GetQueuedWorkItemsForDebugger()
        {
            return ToObjectArray(GetQueuedWorkItems());
        }

        internal static object[] GetGloballyQueuedWorkItemsForDebugger()
        {
            return ToObjectArray(GetGloballyQueuedWorkItems());
        }

        internal static object[] GetLocallyQueuedWorkItemsForDebugger()
        {
            return ToObjectArray(GetLocallyQueuedWorkItems());
        }


        #region UnfairSemaphore implementation

        // This class has been translated from:
        // https://github.com/dotnet/coreclr/blob/97433b9d153843492008652ff6b7c3bf4d9ff31c/src/vm/win32threadpool.h#L124

        // UnfairSemaphore is a more scalable semaphore than Semaphore.  It prefers to release threads that have more recently begun waiting,
        // to preserve locality.  Additionally, very recently-waiting threads can be released without an addition kernel transition to unblock
        // them, which reduces latency.
        //
        // UnfairSemaphore is only appropriate in scenarios where the order of unblocking threads is not important, and where threads frequently
        // need to be woken.

        [StructLayout(LayoutKind.Sequential)]
        private sealed class UnfairSemaphore
        {
            public const int MaxWorker = 0x7FFF;

            // We track everything we care about in A 64-bit struct to allow us to 
            // do CompareExchanges on this for atomic updates.
            [StructLayout(LayoutKind.Explicit)]
            private struct SemaphoreState
            {
                //how many threads are currently spin-waiting for this semaphore?
                [FieldOffset(0)]
                public short Spinners;

                //how much of the semaphore's count is availble to spinners?
                [FieldOffset(2)]
                public short CountForSpinners;

                //how many threads are blocked in the OS waiting for this semaphore?
                [FieldOffset(4)]
                public short Waiters;

                //how much count is available to waiters?
                [FieldOffset(6)]
                public short CountForWaiters;

                [FieldOffset(0)]
                public long RawData;
            }

            [StructLayout(LayoutKind.Explicit, Size = 64)]
            private struct CacheLinePadding
            { }

            private readonly Semaphore m_semaphore;

            // padding to ensure we get our own cache line
#pragma warning disable 169
            private readonly CacheLinePadding m_padding1;
            private SemaphoreState m_state;
            private readonly CacheLinePadding m_padding2;
#pragma warning restore 169

            public UnfairSemaphore()
            {
                m_semaphore = new Semaphore(0, short.MaxValue);
            }

            public bool Wait()
            {
                return Wait(Timeout.InfiniteTimeSpan);
            }

            public bool Wait(TimeSpan timeout)
            {
                while (true)
                {
                    SemaphoreState currentCounts = GetCurrentState();
                    SemaphoreState newCounts = currentCounts;

                    // First, just try to grab some count.
                    if (currentCounts.CountForSpinners > 0)
                    {
                        --newCounts.CountForSpinners;
                        if (TryUpdateState(newCounts, currentCounts))
                            return true;
                    }
                    else
                    {
                        // No count available, become a spinner
                        ++newCounts.Spinners;
                        if (TryUpdateState(newCounts, currentCounts))
                            break;
                    }
                }

                //
                // Now we're a spinner.  
                //
                int numSpins = 0;
                const int spinLimitPerProcessor = 50;
                while (true)
                {
                    SemaphoreState currentCounts = GetCurrentState();
                    SemaphoreState newCounts = currentCounts;

                    if (currentCounts.CountForSpinners > 0)
                    {
                        --newCounts.CountForSpinners;
                        --newCounts.Spinners;
                        if (TryUpdateState(newCounts, currentCounts))
                            return true;
                    }
                    else
                    {
                        double spinnersPerProcessor = (double)currentCounts.Spinners / Environment.ProcessorCount;
                        int spinLimit = (int)((spinLimitPerProcessor / spinnersPerProcessor) + 0.5);
                        if (numSpins >= spinLimit)
                        {
                            --newCounts.Spinners;
                            ++newCounts.Waiters;
                            if (TryUpdateState(newCounts, currentCounts))
                                break;
                        }
                        else
                        {
                            //
                            // We yield to other threads using Thread.Sleep(0) rather than the more traditional Thread.Yield().
                            // This is because Thread.Yield() does not yield to threads currently scheduled to run on other
                            // processors.  On a 4-core machine, for example, this means that Thread.Yield() is only ~25% likely
                            // to yield to the correct thread in some scenarios.
                            // Thread.Sleep(0) has the disadvantage of not yielding to lower-priority threads.  However, this is ok because
                            // once we've called this a few times we'll become a "waiter" and wait on the Semaphore, and that will
                            // yield to anything that is runnable.
                            //
                            Thread.Sleep(0);
                            numSpins++;
                        }
                    }
                }

                //
                // Now we're a waiter
                //
                bool waitSucceeded = m_semaphore.WaitOne(timeout);

                while (true)
                {
                    SemaphoreState currentCounts = GetCurrentState();
                    SemaphoreState newCounts = currentCounts;

                    --newCounts.Waiters;

                    if (waitSucceeded)
                        --newCounts.CountForWaiters;

                    if (TryUpdateState(newCounts, currentCounts))
                        return waitSucceeded;
                }
            }

            public void Release()
            {
                Release(1);
            }

            public void Release(short count)
            {
                while (true)
                {
                    SemaphoreState currentState = GetCurrentState();
                    SemaphoreState newState = currentState;

                    short remainingCount = count;

                    // First, prefer to release existing spinners,
                    // because a) they're hot, and b) we don't need a kernel
                    // transition to release them.
                    short spinnersToRelease = Math.Max((short)0, Math.Min(remainingCount, (short)(currentState.Spinners - currentState.CountForSpinners)));
                    newState.CountForSpinners += spinnersToRelease;
                    remainingCount -= spinnersToRelease;

                    // Next, prefer to release existing waiters
                    short waitersToRelease = Math.Max((short)0, Math.Min(remainingCount, (short)(currentState.Waiters - currentState.CountForWaiters)));
                    newState.CountForWaiters += waitersToRelease;
                    remainingCount -= waitersToRelease;

                    // Finally, release any future spinners that might come our way
                    newState.CountForSpinners += remainingCount;

                    // Try to commit the transaction
                    if (TryUpdateState(newState, currentState))
                    {
                        // Now we need to release the waiters we promised to release
                        if (waitersToRelease > 0)
                            m_semaphore.Release(waitersToRelease);

                        break;
                    }
                }
            }

            private bool TryUpdateState(SemaphoreState newState, SemaphoreState currentState)
            {
                if (Interlocked.CompareExchange(ref m_state.RawData, newState.RawData, currentState.RawData) == currentState.RawData)
                {
                    Debug.Assert(newState.CountForSpinners <= MaxWorker, "CountForSpinners is greater than MaxWorker");
                    Debug.Assert(newState.CountForSpinners >= 0, "CountForSpinners is lower than zero");
                    Debug.Assert(newState.Spinners <= MaxWorker, "Spinners is greater than MaxWorker");
                    Debug.Assert(newState.Spinners >= 0, "Spinners is lower than zero");
                    Debug.Assert(newState.CountForWaiters <= MaxWorker, "CountForWaiters is greater than MaxWorker");
                    Debug.Assert(newState.CountForWaiters >= 0, "CountForWaiters is lower than zero");
                    Debug.Assert(newState.Waiters <= MaxWorker, "Waiters is greater than MaxWorker");
                    Debug.Assert(newState.Waiters >= 0, "Waiters is lower than zero");
                    Debug.Assert(newState.CountForSpinners + newState.CountForWaiters <= MaxWorker, "CountForSpinners + CountForWaiters is greater than MaxWorker");

                    return true;
                }

                return false;
            }

            private SemaphoreState GetCurrentState()
            {
                // Volatile.Read of a long can get a partial read in x86 but the invalid
                // state will be detected in TryUpdateState with the CompareExchange.

                SemaphoreState state = new SemaphoreState();
                state.RawData = Volatile.Read(ref m_state.RawData);
                return state;
            }
        }

        #endregion
    }

}