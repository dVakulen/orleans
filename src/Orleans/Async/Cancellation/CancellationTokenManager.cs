using Orleans.Runtime;

namespace Orleans.Async
{
    internal class CancellationTokenManager
    {
        /// <summary>
        /// Sets target grain to the found instances of type GrainCancellationToken
        /// </summary>
        /// <param name="arguments"> Grain method arguments list</param>
        /// <param name="target"> Target grain reference</param>
        public void SetGrainCancellationTokensTarget(object[] arguments, GrainReference target)
        {
            if (arguments == null) return;
            foreach (var argument in arguments)
            {
                if (argument is GrainCancellationToken)
                {
                    ((GrainCancellationToken) argument).AddGrainReference(target);
                }
            }
        }
    }
}
