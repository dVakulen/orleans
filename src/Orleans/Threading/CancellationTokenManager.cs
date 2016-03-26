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
        /// <summary>
        /// Wraps found cancellation tokens into instances of type CancellationTokenWrapper
        /// </summary>
        /// <param name="arguments"></param>
        public void SetGrainCancellationTokensTarget(object[] arguments, GrainReference target)
        {
            if (arguments == null) return;
            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                if (argument is GrainCancellationToken)
                {
                    ((GrainCancellationToken) argument).TargetGrainReference = target;
                }
            }
        }
    }
}
