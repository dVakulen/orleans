using System;

namespace Orleans.Runtime
{
    [Serializable]
    internal class RequestInvocationInfo
    {
        public RequestInvocationInfo(Message message)
        {
            ActivationId = message.TargetActivation;
        }

        public ActivationId ActivationId { get; }
    }

    // used for tracking request invocation history for deadlock detection.
    [Serializable]
    internal sealed class RequestInvocationHistory : RequestInvocationInfo
    {
        public GrainId GrainId { get; }

        public string DebugContext { get; }

        internal RequestInvocationHistory(Message message) : base(message)
        {
            GrainId = message.TargetGrain;
            DebugContext = message.DebugContext;
        }

        public override string ToString()
        {
            return $"RequestInvocationHistory {GrainId}:{ActivationId}:{DebugContext}";
        }
    }
}
