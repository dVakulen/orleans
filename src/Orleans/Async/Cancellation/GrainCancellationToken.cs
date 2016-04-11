using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans.CodeGeneration;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Orleans.Async
{
    /// <summary>
    /// Grain cancellation token that can be passed thought cross-domain boundaries
    /// </summary>
    [Serializable]
    public class GrainCancellationToken : IDisposable
    {
        [NonSerialized]
        private bool _wentThroughSerialization;

        [NonSerialized]
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// References to remote grains to which this token was passed.
        /// </summary>
        [NonSerialized]
        private readonly ConcurrentBag<GrainReference> _targetGrainReferences;


        /// <summary>
        /// Initializes the <see cref="T:Orleans.Async.GrainCancellationToken"/>.
        /// </summary>
        internal GrainCancellationToken(
            Guid id,
            bool canceled)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            if (canceled)
            {
                _cancellationTokenSource.Cancel();
            }

            Id = id;
            WentThroughSerialization = false;
            _targetGrainReferences = new ConcurrentBag<GrainReference>();
        }

        /// <summary>
        /// Unique id of concrete token
        /// </summary>
        internal Guid Id { get; private set; }

        /// <summary>
        /// Underlying cancellation token
        /// </summary>
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

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

        internal bool IsCancellationRequested => _cancellationTokenSource.IsCancellationRequested;

        internal Task Cancel()
        {
            _cancellationTokenSource.Cancel();
            if (!WentThroughSerialization)
            {
                // token have not passed the cross-domain bounds and remote call is not needed
                return TaskDone.Done;
            }

            var cancellationTasks = _targetGrainReferences
                .Select(reference => reference.AsReference<ICancellationSourcesExtension>().CancelTokenSource(this))
                .ToList();
            return Task.WhenAll(cancellationTasks);
        }

        internal void AddGrainReference(GrainReference grainReference)
        {
            _targetGrainReferences.Add(grainReference);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
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
            var gcts = new GrainCancellationTokenSource(tokenId, cancellationRequested);
            gcts.Token.WentThroughSerialization = true;
            return gcts.Token;
        }

        [CopierMethod]
        internal static object CopyGrainCancellationToken(object obj)
        {
            return obj; // CancellationToken is value type
        }

        #endregion
    }
}