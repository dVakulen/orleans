using System;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.Threading
{
    internal interface ICancellationSourcesExtension : IGrainExtension, IGrain
    {
        [AlwaysInterleave]
        Task CancelTokenSource(Guid tokenId);
    }
}
