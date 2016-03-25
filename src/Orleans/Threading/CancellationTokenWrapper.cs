using System;
using System.Threading;
using Orleans.CodeGeneration;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Orleans.Threading
{
    /// <summary>
    /// Used as replacement of CancellationToken during network roundtrips
    /// </summary>
    [Serializable]
    internal class CancellationTokenWrapper
    {
        [NonSerialized]
        private CancellationToken _cancellationToken;

        [NonSerialized]
        private Action<CancellationTokenWrapper> _onSerialization;

        [NonSerialized]
        private bool _wentThroughSerialization;

        public CancellationTokenWrapper(
            Guid id,
            CancellationToken cancellationToken, 
            GrainReference target, 
            Action<CancellationTokenWrapper> onSerialization)
            : this(id, cancellationToken)
        {
            TargetGrainReference = target;
            OnSerialization = onSerialization;
        }

        public CancellationTokenWrapper(Guid id, CancellationToken cancellationToken)
        {
            Id = id;
            CancellationToken = cancellationToken;
            WentThroughSerialization = false;
        }

        /// <summary>
        /// Unique id of concrete token
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Original request target grain reference
        /// </summary>
        public GrainReference TargetGrainReference { get; private set; }

        /// <summary>
        /// Cancellation token
        /// </summary>
        public CancellationToken CancellationToken
        {
            get { return _cancellationToken; }
            private set { _cancellationToken = value; }
        }

        /// <summary>
        /// Shows whether wrapper has went though serialization process or has not
        /// </summary>
        public bool WentThroughSerialization
        { 
            get { return _wentThroughSerialization; } 
            set { _wentThroughSerialization = value; }
        }

        /// <summary>
        /// Action that will be executed during serialization if wrapped token wasn't cancelled
        /// </summary>
        public Action<CancellationTokenWrapper> OnSerialization 
        {
            get { return _onSerialization; } 
            private set { _onSerialization = value; }
        }

        #region Serialization

        [SerializerMethodAttribute]
        internal static void SerializeCancellationTokenWrapper(object obj, BinaryTokenStreamWriter stream, Type expected)
        {
            var ctw = (CancellationTokenWrapper)obj;
            var cancelled = ctw.CancellationToken.IsCancellationRequested;
            if (!cancelled && ctw.OnSerialization != null)
            {
                ctw.OnSerialization(ctw);
            }

            stream.Write(cancelled);
            stream.Write(ctw.Id);
        }

        [DeserializerMethodAttribute]
        internal static object DeserializeCancellationTokenWrapper(Type expected, BinaryTokenStreamReader stream)
        {
            var cancellationRequested = stream.ReadToken() == SerializationTokenType.True;
            var tokenId = stream.ReadGuid();
            return new CancellationTokenWrapper(tokenId, new CancellationToken(cancellationRequested)) { WentThroughSerialization = true };
        }

        [CopierMethodAttribute]
        internal static object CopyCancellationTokenWrapper(object obj)
        {
            return obj; // CancellationToken is value type
        }

        #endregion
    }
}
