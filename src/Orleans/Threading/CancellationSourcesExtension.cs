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
        private readonly Interner<Guid, CancellationTokenSource> _cancellationTokenSources;
        private readonly TimeSpan _cleanupFrequency = TimeSpan.FromMinutes(10);
        private readonly int _defaultInternerCollectionSize = 31;

        public CancellationSourcesExtension()
        {
            _cancellationTokenSources = new Interner<Guid, CancellationTokenSource>(
                _defaultInternerCollectionSize,
                _cleanupFrequency);
        }

        public Task CancelTokenSource(Guid tokenId)
        {
            CancellationTokenSource cts;
            if (!_cancellationTokenSources.TryFind(tokenId, out cts))
            {
                if (_logger.Value.IsWarning)
                {
                    _logger.Value.Warn(ErrorCode.CancellationTokenCancelFailed, "Remote token cancellation failed: token was not found");
                }

                return TaskDone.Done;
            }

            cts.Cancel();
            return TaskDone.Done;
        }

        internal CancellationTokenSource GetOrCreateCancellationTokenSource(Guid tokenId)
        {
            CancellationTokenSource cts;
            if (_cancellationTokenSources.TryFind(tokenId, out cts))
            {
                return cts;
            }

            cts = new CancellationTokenSource();
            _cancellationTokenSources.Intern(tokenId, cts);
            return cts;
        }
    }
}
