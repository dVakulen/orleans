using System;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Threading
{
    /// <summary>
    /// Distributed version of the CancellationTokenSource
    /// </summary>
    public class GrainCancellationTokenSource : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly GrainCancellationToken _grainCancellationToken;

        /// <summary>
        /// Initializes the <see cref="T:Orleans.Threading.GrainCancellationTokenSource"/>.
        /// </summary>
        public GrainCancellationTokenSource() : this(Guid.NewGuid(), false)
        {
        }

        internal GrainCancellationTokenSource(Guid id, bool cancelled)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            if (cancelled)
            {
                _cancellationTokenSource.Cancel();
            }

            _grainCancellationToken = new GrainCancellationToken(id, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Gets the <see cref="Orleans.Threading.GrainCancellationToken">CancellationToken</see>
        /// associated with this <see cref="GrainCancellationTokenSource"/>.
        /// </summary>
        /// <value>The <see cref="Orleans.Threading.GrainCancellationToken">CancellationToken</see>
        /// associated with this <see cref="GrainCancellationTokenSource"/>.</value>
        public GrainCancellationToken Token
        {
            get { return _grainCancellationToken; }
        }

        /// <summary>
        /// Gets whether cancellation has been requested for this <see
        /// cref="Orleans.Threading.GrainCancellationTokenSource">CancellationTokenSource</see>.
        /// </summary>
        /// <value>Whether cancellation has been requested for this <see
        /// cref="Orleans.Threading.GrainCancellationTokenSource">CancellationTokenSource</see>.</value>
        /// <remarks>
        /// <para>
        /// This property indicates whether cancellation has been requested for this token source, such as
        /// due to a call to its
        /// <see cref="Orleans.Threading.GrainCancellationTokenSource.Cancel()">Cancel</see> method.
        /// </para>
        /// <para>
        /// If this property returns true, it only guarantees that cancellation has been requested. It does not
        /// guarantee that every handler registered with the corresponding token has finished executing, nor
        /// that cancellation requests have finished propagating to all registered handlers and remote targets. Additional
        /// synchronization may be required, particularly in situations where related objects are being
        /// canceled concurrently.
        /// </para>
        /// </remarks>
        public bool IsCancellationRequested
        {
            get { return _cancellationTokenSource.IsCancellationRequested; }
        }

        /// <summary>
        /// Communicates a request for cancellation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The associated <see cref="T:Orleans.Threading.CancellationToken" /> will be
        /// notified of the cancellation and will transition to a state where 
        /// <see cref="Orleans.Threading.CancellationToken.IsCancellationRequested">IsCancellationRequested</see> returns true. 
        /// Any callbacks or cancelable operations
        /// registered with the <see cref="T:Orleans.Threading.CancellationToken"/>  will be executed.
        /// </para>
        /// <para>
        /// Cancelable operations and callbacks registered with the token should not throw exceptions.
        /// However, this overload of Cancel will aggregate any exceptions thrown into a <see cref="Orleans.AggregateException"/>,
        /// such that one callback throwing an exception will not prevent other registered callbacks from being executed.
        /// </para>
        /// <para>
        /// The <see cref="T:Orleans.Threading.ExecutionContext"/> that was captured when each callback was registered
        /// will be reestablished when the callback is invoked.
        /// </para>
        /// </remarks>
        /// <exception cref="T:Orleans.AggregateException">An aggregate exception containing all the exceptions thrown
        /// by the registered callbacks on the associated <see cref="T:Orleans.Threading.CancellationToken"/>.</exception>
        /// <exception cref="T:Orleans.ObjectDisposedException">This <see
        /// cref="T:Orleans.Threading.GrainCancellationTokenSource"/> has been disposed.</exception> 
        public Task Cancel()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return TaskDone.Done;
            }

            _cancellationTokenSource.Cancel();
            if (!_grainCancellationToken.WentThroughSerialization)
            {
                // token have not passed the croos-domain bounds and remote call is not needed
                return TaskDone.Done;
            }

            return _grainCancellationToken.TargetGrainReference.AsReference<ICancellationSourcesExtension>().CancelTokenSource(_grainCancellationToken);
        }

        /// <summary>
        /// Schedules a Cancel operation on this <see cref="T:Orleans.Threading.GrainCancellationTokenSource"/>.
        /// </summary>
        /// <param name="millisecondsDelay">The time span to wait before canceling this <see
        /// cref="T:Orleans.Threading.GrainCancellationTokenSource"/>.
        /// </param>
        /// <exception cref="T:Orleans.ObjectDisposedException">The exception thrown when this <see
        /// cref="T:Orleans.Threading.GrainCancellationTokenSource"/> has been disposed.
        /// </exception>
        /// <exception cref="T:Orleans.ArgumentOutOfRangeException">
        /// The exception thrown when <paramref name="millisecondsDelay"/> is less than -1.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The countdown for the millisecondsDelay starts during this call.  When the millisecondsDelay expires, 
        /// this <see cref="T:Orleans.Threading.GrainCancellationTokenSource"/> is canceled, if it has
        /// not been canceled already.
        /// </para>
        /// </remarks>
        public async Task CancelAfter(int millisecondsDelay)
        {
            await Task.Delay(millisecondsDelay);
            await Cancel();
        }

        /// <summary>
        /// Schedules a Cancel operation on this <see cref="T:Orleans.Threading.GrainCancellationTokenSource"/>.
        /// </summary>
        /// <param name="delay">The time span to wait before canceling this <see
        /// cref="T:Orleans.Threading.GrainCancellationTokenSource"/>.
        /// </param>
        /// <remarks>
        /// <para>
        /// The countdown for the millisecondsDelay starts during this call.  When the millisecondsDelay expires, 
        /// this <see cref="T:Orleans.Threading.GrainCancellationTokenSource"/> is canceled, if it has
        /// not been canceled already.
        /// </para>
        /// </remarks>
        public async void CancelAfter(TimeSpan delay)
        {
            await Task.Delay(delay);
            await Cancel();
        }

        /// <summary>
        /// Releases the resources used by this <see cref="T:Orleans.Threading.GrainCancellationTokenSource" />.
        /// </summary>
        /// <remarks>
        /// This method is not thread-safe for any other concurrent calls.
        /// </remarks>
        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
