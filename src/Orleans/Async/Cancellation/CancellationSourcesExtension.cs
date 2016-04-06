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
        private static readonly Interner<Guid, GrainCancellationToken> _cancellationTokens;
        private static readonly TimeSpan _cleanupFrequency = TimeSpan.FromMinutes(3);
        private static readonly int _defaultInternerCollectionSize = 31;

        static CancellationSourcesExtension()
        {
            _cancellationTokens = new Interner<Guid, GrainCancellationToken>(
                _defaultInternerCollectionSize,
                _cleanupFrequency);
        }

        public Task CancelTokenSource(Guid tokenId)
        {
            GrainCancellationToken gct;
            if (!_cancellationTokens.TryFind(tokenId, out gct))
            {
                _logger.Value.Error(ErrorCode.CancellationTokenCancelFailed, "Remote token cancellation failed: token was not found");
                return TaskDone.Done;
            }

            return gct.Cancel();
        }

        internal GrainCancellationToken GetOrCreateCancellationToken(GrainCancellationToken token)
        {
            return _cancellationTokens.FindOrCreate(token.Id, () =>
            {
                token.WentThroughSerialization = false;
                return token;
            });
        }
    }
}