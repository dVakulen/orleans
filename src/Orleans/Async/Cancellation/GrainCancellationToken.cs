using System;
using System.Threading;
using Orleans.CodeGeneration;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Orleans.Async
{
    /// <summary>
    /// Grain cancellation token that can be passed thought cross-domain boundaries
    /// </summary>
    [Serializable]
    public class GrainCancellationToken
    {
        [NonSerialized]
        private CancellationToken _cancellationToken;

        [NonSerialized]
        private bool _wentThroughSerialization;

        /// <summary>
        /// Initializes the <see cref="T:Orleans.Async.GrainCancellationToken"/>.
        /// </summary>
        internal GrainCancellationToken(
            Guid id,
            CancellationToken cancellationToken, 
            GrainReference target)
            : this(id, cancellationToken)
        {
            TargetGrainReference = target;
        }

        /// <summary>
        /// Initializes the <see cref="T:Orleans.Async.GrainCancellationToken"/>.
        /// </summary>
        internal GrainCancellationToken(Guid id, CancellationToken cancellationToken)
        {
            Id = id;
            CancellationToken = cancellationToken;
            WentThroughSerialization = false;
        }

        /// <summary>
        /// Unique id of concrete token
        /// </summary>
        internal Guid Id { get; private set; }

        /// <summary>
        /// Original request target grain reference.
        /// </summary>
        internal GrainReference TargetGrainReference { get; set; }

        /// <summary>
        /// Underlying cancellation token
        /// </summary>
        public CancellationToken CancellationToken
        {
            get { return _cancellationToken; }
            private set { _cancellationToken = value; }
        }

        /// <summary>
        /// Shows whether wrapper has went though serialization process or not
        /// Exists for local case optimization: there's no need to issue CancellationSourcesExtension cancel call 
        /// if the token haven't crossed cross-domain boundaries.
        /// </summary>
        internal bool WentThroughSerialization
        { 
            get { return _wentThroughSerialization; } 
            set { _wentThroughSerialization = value; }
        }

        #region Serialization

        [SerializerMethod]
        internal static void SerializeGrainCancellationToken(object obj, BinaryTokenStreamWriter stream, Type expected)
        {
            var ctw = (GrainCancellationToken)obj;
            ctw.WentThroughSerialization = true;
            var canceled = ctw.CancellationToken.IsCancellationRequested;
            stream.Write(canceled);
            stream.Write(ctw.Id);
        }

        [DeserializerMethod]
        internal static object DeserializeGrainCancellationToken(Type expected, BinaryTokenStreamReader stream)
        {
            var cancellationRequested = stream.ReadToken() == SerializationTokenType.True;
            var tokenId = stream.ReadGuid();
            return new GrainCancellationToken(tokenId, new CancellationToken(cancellationRequested)) { WentThroughSerialization = true };
        }

        [CopierMethod]
        internal static object CopyGrainCancellationToken(object obj)
        {
            return obj; // CancellationToken is value type
        }

        #endregion
    }
}
