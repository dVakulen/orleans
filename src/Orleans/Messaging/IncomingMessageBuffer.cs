
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Orleans.Runtime
{
    internal class IncomingMessageBuffer
    {
        internal const int DEFAULT_RECEIVE_BUFFER_SIZE = 128 * Kb; // 128k
        protected const int Kb = 1024;
        protected const int DEFAULT_MAX_SUSTAINED_RECEIVE_BUFFER_SIZE = 1024 * Kb; // 1mg
        protected const int GROW_MAX_BLOCK_SIZE = 1024 * Kb; // 1mg
        protected List<ArraySegment<byte>> readBuffer;//readonly
        private List<ArraySegment<byte>> tempBuffer;//readonly
        protected readonly int maxSustainedBufferSize;
        protected int currentBufferSize;
        protected readonly byte[] lengthBuffer;
        private readonly MessagePrefixHolder prefixHolder = new MessagePrefixHolder();
        protected int headerLength;
        protected int bodyLength;

        protected int receiveOffset;
        protected int decodeOffset;

        protected readonly bool supportForwarding;
        protected Logger Log;

        protected IncomingMessageBuffer(Logger logger,
            bool supportForwarding = false,
            int receiveBufferSize = DEFAULT_RECEIVE_BUFFER_SIZE,
            int maxSustainedReceiveBufferSize = DEFAULT_MAX_SUSTAINED_RECEIVE_BUFFER_SIZE)
        {
            Log = logger;
            this.supportForwarding = supportForwarding;
            currentBufferSize = receiveBufferSize;
            maxSustainedBufferSize = maxSustainedReceiveBufferSize;
            lengthBuffer = new byte[Message.LENGTH_HEADER_SIZE];
            receiveOffset = 0;
            decodeOffset = 0;
            headerLength = 0;
            bodyLength = 0;
        }

        public IncomingMessageBuffer(
            Logger logger,
         List<ArraySegment<byte>> readBuf,
         bool supportForwarding = false,
         int receiveBufferSize = DEFAULT_RECEIVE_BUFFER_SIZE,
         int maxSustainedReceiveBufferSize = DEFAULT_MAX_SUSTAINED_RECEIVE_BUFFER_SIZE)
            : this(logger, supportForwarding, receiveBufferSize, maxSustainedReceiveBufferSize)
        {
            readBuffer = readBuf;
        }

        public virtual void UpdateReceivedData(int bytesRead)
        {
            receiveOffset = bytesRead;
            decodeOffset = 0;
            if (decodeOffset >= prefixHolder.Count && prefixHolder.HasPrefix)
            {
                decodeOffset -= prefixHolder.Count;
                decodeOffset = 0;
                prefixHolder.Reset();
            }
        }

        public void Reset()
        {
            receiveOffset = 0;
            decodeOffset = 0;
            headerLength = 0;
            bodyLength = 0;
        }

        public virtual bool TryDecodeMessage(out Message msg)
        {
            msg = null;
            if (decodeOffset == receiveOffset)
            {
                return false;
            }

            // Is there enough read into the buffer to continue (at least read the lengths?)
            if (TryHandlePrefix()) return false;

            // parse lengths if needed
            if (headerLength == 0 || bodyLength == 0)
            {
                // get length segments
                List<ArraySegment<byte>> lenghts = BuildSegmentListWithLengthLimit(Message.LENGTH_HEADER_SIZE);

                // copy length segment to buffer
                int lengthBufferoffset = 0;
                foreach (ArraySegment<byte> seg in lenghts)
                {
                    Buffer.BlockCopy(seg.Array, seg.Offset, lengthBuffer, lengthBufferoffset, seg.Count);
                    lengthBufferoffset += seg.Count;
                }

                // read lengths
                headerLength = BitConverter.ToInt32(lengthBuffer, 0);
                bodyLength = BitConverter.ToInt32(lengthBuffer, 4);
            }

            // Is there enough read into the buffer to read full message
            if (TryHandlePrefix()) return false;

            // decode header
            List<ArraySegment<byte>> header = BuildSegmentListWithLengthLimit(headerLength);

            // decode body
            List<ArraySegment<byte>> body = BuildSegmentListWithLengthLimit(bodyLength);

            // need to maintain ownership of buffer, so if we are supporting forwarding we need to duplicate the body buffer.
            if (supportForwarding)
            {
                body = DuplicateBuffer(body);
            }

            // build message
            msg = new Message(header, body, !supportForwarding);

            MessagingStatisticsGroup.OnMessageReceive(msg, headerLength, bodyLength);

            LogMessageSize(msg);

            // update parse receiveOffset and clear lengths
            headerLength = 0;
            bodyLength = 0;

            return true;
        }

        protected virtual int CalculateKnownMessageSize()
        {
            return headerLength + bodyLength;
        }

        protected List<ArraySegment<byte>> DuplicateBuffer(List<ArraySegment<byte>> body)
        {
            var dupBody = new List<ArraySegment<byte>>(body.Count);
            foreach (ArraySegment<byte> seg in body)
            {
                var dupSeg = new ArraySegment<byte>(BufferPool.GlobalPool.GetBuffer(), seg.Offset, seg.Count);
                Buffer.BlockCopy(seg.Array, seg.Offset, dupSeg.Array, dupSeg.Offset, seg.Count);
                dupBody.Add(dupSeg);
            }
            return dupBody;
        }

        private void LogMessageSize(Message msg)
        {
            if (headerLength + bodyLength > Message.LargeMessageSizeThreshold)
            {
                Log.Info(ErrorCode.Messaging_LargeMsg_Incoming,
                    "Receiving large message Size={0} HeaderLength={1} BodyLength={2}. Msg={3}",
                    headerLength + bodyLength, headerLength, bodyLength, msg.ToString());
                if (Log.IsVerbose3) Log.Verbose3("Received large message {0}", msg.ToLongString());
            }
        }

        private bool TryHandlePrefix()
        {
            if (AvailableReadLength < CalculateKnownMessageSize())
            {
                prefixHolder.HandlePrefix(readBuffer, decodeOffset, receiveOffset - decodeOffset);
                return true;
            }

            return false;
        }

        private List<ArraySegment<byte>> BuildSegmentListWithLengthLimit(int length)
        {
            if (!prefixHolder.HasPrefix)
            {
                var res = ByteArrayBuilder.BuildSegmentListWithLengthLimit(readBuffer, decodeOffset, length);

                decodeOffset += length;
                return res;
            }

            int bytesRead;
            var result = new List<ArraySegment<byte>>(readBuffer.Count);
            ByteArrayBuilder.BuildSegmentListWithLengthLimit(
                result,
                prefixHolder.TryGetPrefix(),
                decodeOffset,
                length,
                out bytesRead);

            // decrease
            length -= bytesRead;
            decodeOffset += bytesRead;

            if (length <= 0)
            {
                if (length == prefixHolder.Count)
                {
                    throw new Exception("q");
                }
                return result;
            }

            if (bytesRead > 0)
            {
                decodeOffset = 0;
                prefixHolder.Reset();
            }

            ByteArrayBuilder.BuildSegmentListWithLengthLimit(
                result,
                readBuffer,
                decodeOffset,
                length,
                out bytesRead);

            decodeOffset += bytesRead;
            return result;
        }

        private int AvailableReadLength => prefixHolder.Count + receiveOffset - decodeOffset;

        private class MessagePrefixHolder
        {
            private List<ArraySegment<byte>> buf;
            public bool HasPrefix { get; private set; }
            public int Count { get; private set; }

            public void HandlePrefix(List<ArraySegment<byte>> buffer, int offset, int remainingBytesToProcess)
            {
                if (remainingBytesToProcess == 0)
                    return;
                HasPrefix = true;
                Count += remainingBytesToProcess;
                var length = remainingBytesToProcess;
                buf = buf ?? new List<ArraySegment<byte>>();//BufferPool.GlobalPool.GetMultiBuffer(remainingBytesToProcess);
                var lengthSoFar = 0;
                var countSoFar = 0;
                foreach (var segment in buffer)
                {
                    var bytesStillToSkip = offset - lengthSoFar;
                    lengthSoFar += segment.Count;

                    if (segment.Count <= bytesStillToSkip) // Still skipping past this buffer
                    {
                        continue;
                    }
                    if (bytesStillToSkip > 0) // This is the first buffer
                    {
                        var count = Math.Min(length - countSoFar, segment.Count - bytesStillToSkip);
                        var seg = new ArraySegment<byte>(BufferPool.GlobalPool.GetBuffer(), 0, count);
                        Buffer.BlockCopy(segment.Array, bytesStillToSkip, seg.Array, 0, count);
                        buf.Add(seg);
                        // buf.Add(new ArraySegment<byte>(segment.Array, bytesStillToSkip, Math.Min(length - countSoFar, segment.Count - bytesStillToSkip)));
                        countSoFar += count;
                    }
                    else
                    {
                        var count = Math.Min(length - countSoFar, segment.Count);
                        var seg = new ArraySegment<byte>(BufferPool.GlobalPool.GetBuffer(), 0, count);

                        Buffer.BlockCopy(segment.Array, 0, seg.Array, 0, count);
                        buf.Add(seg);
                        countSoFar += count;
                    }

                    if (countSoFar == length)
                    {
                        break;
                    }
                }
            }

            public List<ArraySegment<byte>> TryGetPrefix()
            {
                return buf;
            }

            public void Reset()
            {
                Count = 0;
                HasPrefix = false;
                BufferPool.GlobalPool.Release(buf);
                buf = null;
            }
        }
    }

    internal sealed class ClientIncomingMessageBuffer : IncomingMessageBuffer
    {
        public ClientIncomingMessageBuffer(
            Logger logger,
            bool supportForwarding = false,
            int receiveBufferSize = DEFAULT_RECEIVE_BUFFER_SIZE,
            int maxSustainedReceiveBufferSize = 1048576)
            : base(logger, BufferPool.GlobalPool.GetMultiBuffer(receiveBufferSize), supportForwarding, receiveBufferSize, maxSustainedReceiveBufferSize)
        {
        }

        public List<ArraySegment<byte>> BuildReceiveBuffer()
        {
            // Opportunistic reset to start of buffer
            if (decodeOffset == receiveOffset)
            {
                decodeOffset = 0;
                receiveOffset = 0;
            }

            return ByteArrayBuilder.BuildSegmentList(readBuffer, receiveOffset);
        }

        public override void UpdateReceivedData(int bytesRead)
        {
            receiveOffset += bytesRead;
        }

        public override bool TryDecodeMessage(out Message msg)
        {
            msg = null;

            // Is there enough read into the buffer to continue (at least read the lengths?)
            if (receiveOffset - decodeOffset < CalculateKnownMessageSize())
            {
                return false;
            }

            // parse lengths if needed
            if (headerLength == 0 || bodyLength == 0)
            {
                // get length segments
                List<ArraySegment<byte>> lenghts = ByteArrayBuilder.BuildSegmentListWithLengthLimit(readBuffer, decodeOffset, Message.LENGTH_HEADER_SIZE);

                // copy length segment to buffer
                int lengthBufferoffset = 0;
                foreach (ArraySegment<byte> seg in lenghts)
                {
                    Buffer.BlockCopy(seg.Array, seg.Offset, lengthBuffer, lengthBufferoffset, seg.Count);
                    lengthBufferoffset += seg.Count;
                }

                // read lengths
                headerLength = BitConverter.ToInt32(lengthBuffer, 0);
                bodyLength = BitConverter.ToInt32(lengthBuffer, 4);
            }

            // If message is too big for current buffer size, grow
            while (decodeOffset + CalculateKnownMessageSize() > currentBufferSize)
            {
                GrowBuffer();
            }

            // Is there enough read into the buffer to read full message

            if (receiveOffset - decodeOffset < CalculateKnownMessageSize())
            {
                return false;
            }

            // decode header
            int headerOffset = decodeOffset + Message.LENGTH_HEADER_SIZE;
            List<ArraySegment<byte>> header = ByteArrayBuilder.BuildSegmentListWithLengthLimit(readBuffer, headerOffset, headerLength);

            // decode body
            int bodyOffset = headerOffset + headerLength;
            List<ArraySegment<byte>> body = ByteArrayBuilder.BuildSegmentListWithLengthLimit(readBuffer, bodyOffset, bodyLength);

            // need to maintain ownership of buffer, so if we are supporting forwarding we need to duplicate the body buffer.
            if (supportForwarding)
            {
                body = DuplicateBuffer(body);
            }

            // build message
            msg = new Message(header, body, !supportForwarding);

            MessagingStatisticsGroup.OnMessageReceive(msg, headerLength, bodyLength);

            if (headerLength + bodyLength > Message.LargeMessageSizeThreshold)
            {
                Log.Info(ErrorCode.Messaging_LargeMsg_Incoming, "Receiving large message Size={0} HeaderLength={1} BodyLength={2}. Msg={3}",
                    headerLength + bodyLength, headerLength, bodyLength, msg.ToString());
                if (Log.IsVerbose3) Log.Verbose3("Received large message {0}", msg.ToLongString());
            }

            // update parse receiveOffset and clear lengths
            decodeOffset = bodyOffset + bodyLength;
            headerLength = 0;
            bodyLength = 0;
            AdjustBuffer();

            return true;
        }

        protected override int CalculateKnownMessageSize()
        {
            return headerLength + bodyLength + Message.LENGTH_HEADER_SIZE;
        }
        protected void GrowBuffer()
        {
            //TODO: Add configurable max message size for safety
            //TODO: Review networking layer and add max size checks to all dictionaries, arrays, or other variable sized containers.
            // double buffer size up to max grow block size, then only grow it in those intervals
            int growBlockSize = Math.Min(currentBufferSize, GROW_MAX_BLOCK_SIZE);
            readBuffer.AddRange(BufferPool.GlobalPool.GetMultiBuffer(growBlockSize));
            currentBufferSize += growBlockSize;
        }

        /// <summary>
        /// This call cleans up the buffer state to make it optimal for next read.
        /// The leading chunks, used by any processed messages, are removed from the front
        ///   of the buffer and added to the back.   Decode and receiver offsets are adjusted accordingly.
        /// If the buffer was grown over the max sustained buffer size (to read a large message) it is shrunken.
        /// </summary>
        private void AdjustBuffer()
        {
            int consumedBytes = 0;
            while (readBuffer.Count != 0)
            {
                ArraySegment<byte> seg = readBuffer[0];
                if (seg.Count <= decodeOffset - consumedBytes)
                {
                    consumedBytes += seg.Count;
                    readBuffer.Remove(seg);
                    BufferPool.GlobalPool.Release(seg.Array);
                }
                else
                {
                    break;
                }
            }
            decodeOffset -= consumedBytes;
            receiveOffset -= consumedBytes;

            // backfill any consumed buffers, to preserve buffer size.
            if (consumedBytes != 0)
            {
                int backfillBytes = consumedBytes;
                // If buffer is larger than max sustained size, backfill only up to max sustained buffer size.
                if (currentBufferSize > maxSustainedBufferSize)
                {
                    backfillBytes = Math.Max(consumedBytes + maxSustainedBufferSize - currentBufferSize, 0);
                    currentBufferSize -= consumedBytes;
                    currentBufferSize += backfillBytes;
                }
                if (backfillBytes > 0)
                {
                    readBuffer.AddRange(BufferPool.GlobalPool.GetMultiBuffer(backfillBytes));
                }
            }
        }
    }

    internal class CircularByteBuffer
    {
        private const int Kb = 1024;
        private const int DEFAULT_BUFFER_SIZE = 128 * Kb; // 128k
        private const int DEFAULT_MAX_SUSTAINED_RECEIVE_BUFFER_SIZE = 1024 * Kb; // 1mg
        private const int GROW_MAX_BLOCK_SIZE = 1024 * Kb; // 1mg
        private readonly LinkedList<ArraySegment<byte>> readBuffer;
        private LinkedListNode<ArraySegment<byte>> CurrentNode;
        private readonly int maxSustainedBufferSize;
        private int currentBufferSize;

        private int decodeOffset;
        private int length;

        private readonly bool supportForwarding;

        public CircularByteBuffer(
            int bufferSize = DEFAULT_BUFFER_SIZE,
            int maxSustainedReceiveBufferSize = DEFAULT_MAX_SUSTAINED_RECEIVE_BUFFER_SIZE)
        {
            currentBufferSize = bufferSize;
            maxSustainedBufferSize = maxSustainedReceiveBufferSize;
            FillBufferFromPool(currentBufferSize);
            decodeOffset = 0;
            length = 0;
        }

        public void UpdateReceivedData(int bytesRead)
        {
            //  receiveOffset += bytesRead;
        }

        public void Reset()
        {
            // receiveOffset = 0;
            decodeOffset = 0;
        }


        /// <summary>
        /// This call cleans up the buffer state to make it optimal for next read.
        /// </summary>
        private void AdjustBuffer()
        {
            while (currentBufferSize > maxSustainedBufferSize && readBuffer.Count != 0 && decodeOffset > readBuffer.First.Value.Count)
            {
                var segment = readBuffer.First;
                readBuffer.RemoveFirst();
                var sizeToRemove = segment.Value.Count;
                currentBufferSize -= sizeToRemove;
                decodeOffset -= sizeToRemove;
                BufferPool.GlobalPool.Release(segment.Value.Array);
            }
        }
        public void HandlePrefix(List<ArraySegment<byte>> buffer, int offset, int remainingBytesToProcess)
        {
            if (remainingBytesToProcess == 0)
                return;
            length += remainingBytesToProcess;
            var lengthSoFar = 0;
            var countSoFar = 0;

            EnsureBufferCapacity(remainingBytesToProcess);

            foreach (var sourceSegment in buffer)
            {
                var bytesStillToSkip = offset - lengthSoFar;
                lengthSoFar += sourceSegment.Count;

                if (sourceSegment.Count <= bytesStillToSkip) // Still skipping past this buffer
                {
                    continue;
                }

                if (bytesStillToSkip > 0) // This is the first buffer
                {
                    var count = Math.Min(length - countSoFar, sourceSegment.Count - bytesStillToSkip);
                    var seg = new ArraySegment<byte>(BufferPool.GlobalPool.GetBuffer(), 0, count);
                    Buffer.BlockCopy(sourceSegment.Array, bytesStillToSkip, seg.Array, 0, count);
                    //   buf.Add(seg);
                    // buf.Add(new ArraySegment<byte>(segment.Array, bytesStillToSkip, Math.Min(length - countSoFar, segment.Count - bytesStillToSkip)));
                    countSoFar += count;
                }
                else
                {
                    var count = Math.Min(length - countSoFar, sourceSegment.Count);
                    var seg = new ArraySegment<byte>(BufferPool.GlobalPool.GetBuffer(), 0, count);

                    Buffer.BlockCopy(sourceSegment.Array, 0, seg.Array, 0, count);
                    //  buf.Add(seg);
                    countSoFar += count;
                }

                if (countSoFar == length)
                {
                    break;
                }
            }
        }

        private void EnsureBufferCapacity(int remainingBytesToProcess)
        {
            while (currentBufferSize - decodeOffset < remainingBytesToProcess)
            {
                GrowBuffer();
            }
        }

        private void GrowBuffer()
        {
            // double buffer size up to max grow block size, then only grow it in those intervals
            int growBlockSize = Math.Min(currentBufferSize, GROW_MAX_BLOCK_SIZE);
            FillBufferFromPool(growBlockSize);
            currentBufferSize += growBlockSize;
        }

        private void FillBufferFromPool(int size)
        {
            var buf = BufferPool.GlobalPool.GetMultiBuffer(size);
            foreach (var b in buf)
            {
                readBuffer.AddLast(b);
            }
        }
    }
}