using System;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Async
{
    /// <summary>
    /// Contains list of cancellation token source corresponding to the tokens
    /// passed to the related grain activation.
    /// </summary>
    internal class CancellationSourcesExtension : ICancellationSourcesExtension
    {
        private readonly Lazy<TraceLogger> _logger = new Lazy<TraceLogger>(() =>
            TraceLogger.GetLogger("CancellationSourcesExtension", TraceLogger.LoggerType.Application));
        private static readonly Interner<Guid, GrainCancellationTokenSource> _cancellationTokenSources;
        private static readonly TimeSpan _cleanupFrequency = TimeSpan.FromMinutes(3);
        private static readonly int _defaultInternerCollectionSize = 31;

        static CancellationSourcesExtension()
        {
            _cancellationTokenSources = new Interner<Guid, GrainCancellationTokenSource>(
                _defaultInternerCollectionSize,
                _cleanupFrequency);
        }

        public Task CancelTokenSource(GrainCancellationToken token)
        {
            GrainCancellationTokenSource cts;
            if (!_cancellationTokenSources.TryFind(token.Id, out cts))
            {
                _logger.Value.Error(ErrorCode.CancellationTokenCancelFailed, "Remote token cancellation failed: token was not found");
                return TaskDone.Done;
            }

            return cts.Cancel();
        }
        
        internal GrainCancellationToken GetOrCreateCancellationToken(Guid tokenId, bool cancelled)
        {
            var cts = _cancellationTokenSources.FindOrCreate(tokenId, () =>
            {
                var grainCts = new GrainCancellationTokenSource(tokenId, cancelled);

                // capture the reference so that GrainCancellationTokenSource will not be collected 
                // earlier than underlying CancellationTokenSource
                grainCts.Token.CancellationToken.Register((a) =>
                {
                    var captured = grainCts;
                }, grainCts);

                return grainCts;
            });
            return cts.Token;
        }
    }
}
