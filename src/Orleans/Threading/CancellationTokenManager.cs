using Orleans.Runtime;

namespace Orleans.Threading
{
    internal class CancellationTokenManager
    {
        /// <summary>
        /// Wraps found cancellation tokens into instances of type CancellationTokenWrapper
        /// </summary>
        /// <param name="arguments"> Grain method arguments list</param>
        /// <param name="target"> Target grain reference</param>
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
