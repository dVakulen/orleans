using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Threading
{
    /// <summary>
    /// Rationale: on invoking the GrainReference method the CancellationTokens from method signature being wrapped in CancellationTokenWrapper.
    /// If request is local - before invoking of actual grain method it's just being unwrapped.
    /// For the remote case: on message serialization subscription on token cancel event,  
    /// which will cancel linked CancellationTokenSource located in the CancellationTokenExtension of the target grain is being created.
    /// </summary>
    internal class CancellationTokenManager
    {
        private static readonly IBackoffProvider DefaultBackoffProvider = new FixedBackoff(TimeSpan.FromSeconds(1));
     
        private TraceLogger logger = TraceLogger.GetLogger("CancellationTokenManager", TraceLogger.LoggerType.Runtime);
        private Func<Exception, int, bool> errorFilter = ((exc, i) => true);

        /// <summary>
        /// Wraps found cancellation tokens into instances of type CancellationTokenWrapper
        /// </summary>
        /// <param name="arguments"></param>
        public void WrapCancellationTokens(object[] arguments, GrainReference target)
        {
            if (arguments == null) return;
            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                if (argument is CancellationToken)
                {
                    arguments[i] = WrapCancellationToken((CancellationToken)argument, target);
                }
            }
        }

        /// <summary>
        /// Wraps cancellation token into CancellationTokenWrapper
        /// </summary>
        /// <param name="ct"> Cancellation token to be wrapped</param>
        /// <returns>CancellationTokenWrapper</returns>
        public CancellationTokenWrapper WrapCancellationToken(CancellationToken ct, GrainReference target)
        {
            var tokenId = Guid.NewGuid();
            return new CancellationTokenWrapper(tokenId, ct, target, (ctw) => RegisterTokenCallbacks(ctw));
        }

        internal void RegisterTokenCallbacks(CancellationTokenWrapper ctw)
        {
            var ct = ctw.CancellationToken;
            if (!ct.CanBeCanceled) return;
            ct.Register(() => Cancel(ctw).Ignore());
        }

        private async Task Cancel(CancellationTokenWrapper ctw)
        {
            try
            {
                var extensionReference = ctw.TargetGrainReference.AsReference<ICancellationSourcesExtension>();
                await AsyncExecutorWithRetries
                    .ExecuteWithRetries(i =>
                       extensionReference.CancelTokenSource(ctw.Id), 
                       3,
                       errorFilter,
                       TimeSpan.MaxValue, 
                       DefaultBackoffProvider);
            }
            catch (Exception ex)
            {
                if (logger.IsWarning)
                {
                    logger.Warn(
                        ErrorCode.CancellationTokenCancelFailed,
                        string.Format("Remote token cancellation failed for target {0}", ctw.TargetGrainReference),
                        ex);
                }
            }
        }
    }
}
