using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Orleans.Runtime
{
    public class DisposeHandle<T>
        where T : class, IDisposable
    {
        private IObjectPool<T> pool;
        private T tt;

        public DisposeHandle(IObjectPool<T> pool, T t)
        {
            tt = t;
            this.pool = pool;
        }

        public void Dispose()
        {
            var z = Interlocked.Exchange(ref pool, null);
            if (z != null)
            {
                z.Free(tt);
            }
        }
    }
    /// <summary>
    /// Utility class to support pooled objects by allowing them to track the pook they came from and return to it when disposed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PooledResource<T>
        where T : class, IDisposable
    {
        private readonly IObjectPool<T> _pool;
        internal int Disposed;
        public DisposeHandle<T> Handle; 

        /// <summary>
        /// Pooled resource that is from the provided pool
        /// </summary>
        /// <param name="pool"></param>
        protected PooledResource(IObjectPool<T> pool)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            this._pool = pool;
        }

        /// <summary>
        /// If this object is to be used in a fixed size object pool, this call should be
        ///   overridden with the purge implementation that returns the object to the pool.
        /// </summary>
        public virtual void SignalPurge()
        {
            Dispose();
        }

        /// <summary>
        /// Returns item to pool
        /// </summary>
        public void Dispose()
        {
            lock (_pool)
            {
                OnResetState();
                _pool.Free(this as T);
            }
        }

        /// <summary>
        /// Notifies the object that it has been purged, so it can reset itself to
        ///   the state of a newly allocated object.
        /// </summary>
        public virtual void OnResetState()
        {
        }

        public void OnAlloc()
        {

            (this as Message).isRestted = false;
          Handle = new DisposeHandle<T>(_pool, (this as T));
        }
    }

    public class ConcurrentObjectPool<T> : IObjectPool<T>
        where T : PooledResource<T>, IDisposable
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _objectGenerator;

        public ConcurrentObjectPool(Func<T> objectGenerator)
        {
            if (objectGenerator == null) throw new ArgumentNullException(nameof(objectGenerator));
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator;
        }

        public T Allocate()
        {
            return _objectGenerator();
            lock (locker)
            {
                T item;

                if (_objects.TryTake(out item))
                {
                    Interlocked.Exchange(ref item.Disposed, 0);
                    return item;
                }
            }
        }
        public static ConcurrentQueue<Guid> Guids = new ConcurrentQueue<Guid>(); 
        public void Free(T item)
        {
            lock (locker)
            {
                if (Interlocked.Exchange(ref item.Disposed, 1) == 1)
                {
                    Guids.Enqueue((item as Message).Key);
                    throw new Exception($"Tried to free an object of type {item.GetType()} multiple times");
                }

               _objects.Add(item);
            }
        }

        public object locker = new object();
    }
}
