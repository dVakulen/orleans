using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.Threading
{
    internal class CancellationSourcesExtension : ICancellationSourcesExtension
    {
        private readonly Lazy<TraceLogger> _logger = new Lazy<TraceLogger>(() =>
            TraceLogger.GetLogger("CancellationSourcesExtension", TraceLogger.LoggerType.Application));
        private readonly Interner<Guid, GrainCancellationTokenSource> _cancellationTokenSources;
        private readonly TimeSpan _cleanupFrequency = TimeSpan.FromMinutes(10);
        private readonly int _defaultInternerCollectionSize = 31;

        public CancellationSourcesExtension()
        {
            _cancellationTokenSources = new Interner<Guid, GrainCancellationTokenSource>(
                _defaultInternerCollectionSize,
                _cleanupFrequency);
        }

        public Task CancelTokenSource(GrainCancellationToken token)
        {
            return CancelTokenSource(token.Id);
        }

        public Task CancelTokenSource(Guid tokenId)
        {
            GrainCancellationTokenSource cts;
            if (!_cancellationTokenSources.TryFind(tokenId, out cts))
            {
                if (_logger.Value.IsWarning)
                {
                    _logger.Value.Warn(ErrorCode.CancellationTokenCancelFailed, "Remote token cancellation failed: token was not found");
                }

                return TaskDone.Done;
            }

            return cts.Cancel();
        }

        internal GrainCancellationToken GetOrCreateCancellationToken(Guid tokenId, bool cancelled)
        {
            GrainCancellationTokenSource cts = _cancellationTokenSources.FindOrCreate(tokenId,
                () =>
                {
                    var z =
                        new GrainCancellationTokenSource(tokenId, cancelled);
                    z.Token.Register(() =>
                    {
                        var b = z;
                        var f = b;
                    });
                    return z;
                });

            return cts.GrainCancellationToken;
        }
    }
}
