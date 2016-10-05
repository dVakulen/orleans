
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Orleans.Runtime
{
    internal class IncomingMessageBuffer
    {
        private const int Kb = 1024;
        private const int DEFAULT_RECEIVE_BUFFER_SIZE = 128 * Kb; // 128k
        private const int DEFAULT_MAX_SUSTAINED_RECEIVE_BUFFER_SIZE = 1024 * Kb; // 1mg
        private const int GROW_MAX_BLOCK_SIZE = 1024 * Kb; // 1mg
        private  List<ArraySegment<byte>> readBuffer;//readonly
        private List<ArraySegment<byte>> tempBuffer;//readonly
        private readonly int maxSustainedBufferSize;
        private int currentBufferSize;
        static ConcurrentQueue<IncomingMessageBuffer>  Tratata = new ConcurrentQueue<IncomingMessageBuffer>();
        private readonly byte[] lengthBuffer;
        private MessagePrefixHolder prefixHolder = new MessagePrefixHolder();
        private int headerLength;
        private int bodyLength;

        private int receiveOffset;
        private int decodeOffset;

        private readonly bool supportForwarding;
        private bool wtf = false;
        private Logger Log;

        public IncomingMessageBuffer(Logger logger, 
            bool supportForwarding = false,
            int receiveBufferSize = DEFAULT_RECEIVE_BUFFER_SIZE,
            int maxSustainedReceiveBufferSize = DEFAULT_MAX_SUSTAINED_RECEIVE_BUFFER_SIZE,
            IList<ArraySegment<byte>> readBuf = null,
            byte[] bb = null)
        {
            Log = logger;
            this.supportForwarding = supportForwarding;
            currentBufferSize = receiveBufferSize;
            maxSustainedBufferSize = maxSustainedReceiveBufferSize;
            lengthBuffer = new byte[Message.LENGTH_HEADER_SIZE];
            wtf = readBuf is List<ArraySegment<byte>> || bb != null;
            readBuffer = readBuf as List<ArraySegment<byte>> ?? BufferPool.GlobalPool.GetMultiBuffer(currentBufferSize); // readBuf as List<ArraySegment<byte>> ?? bb != null ? new List<ArraySegment<byte>>{new ArraySegment<byte>(bb)}:
            receiveOffset = 0;
            decodeOffset = 0;
            headerLength = 0;
            bodyLength = 0;
            Tratata.Enqueue(this);
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
        public void UpdateReceivedData(int bytesRead)
        {
            if (wtf)
            {
                receiveOffset = 0;
                decodeOffset = 0;
            }
               receiveOffset += bytesRead;
        }

        public void Reset()
        {
            receiveOffset = 0;
            decodeOffset = 0;
            headerLength = 0;
            bodyLength = 0;
        }

        public bool TryDecodeMessage(out Message msg)
        {
            msg = null;

            bool prefixUsed = false;
            // Is there enough read into the buffer to continue (at least read the lengths?)
            if (receiveOffset - decodeOffset < CalculateKnownMessageSize())
            {
                if (!prefixUsed && wtf && prefixHolder.HasPrefix && prefixHolder.Count + receiveOffset - decodeOffset > CalculateKnownMessageSize())
                {
                    var t = readBuffer;
                    readBuffer = new List<ArraySegment<byte>>();
                    readBuffer.AddRange(prefixHolder.TryGetPrefix());
                    readBuffer.AddRange(t);
                    tempBuffer = t;
                    receiveOffset += prefixHolder.Count;
                    prefixUsed = true;
                }
                else
                {
                    if (wtf)
                        prefixHolder.HandlePrefix(readBuffer, decodeOffset, receiveOffset - decodeOffset);
                    return false;
                }
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
            while (!wtf && decodeOffset + CalculateKnownMessageSize() > currentBufferSize)
            {
                GrowBuffer();
            }

            if (!prefixUsed && wtf && prefixHolder.HasPrefix)
            {
                var t = readBuffer;
                readBuffer = new List<ArraySegment<byte>>();
                readBuffer.AddRange(prefixHolder.TryGetPrefix());
                readBuffer.AddRange(t);
                tempBuffer = t;
                receiveOffset += prefixHolder.Count;
                prefixUsed = true;
            }

                // Is there enough read into the buffer to read full message
           
            if (receiveOffset - decodeOffset < CalculateKnownMessageSize())
            {
                if (!prefixUsed && wtf && prefixHolder.HasPrefix && prefixHolder.Count + receiveOffset - decodeOffset > CalculateKnownMessageSize())
                {
                    var t = readBuffer;
                    readBuffer = new List<ArraySegment<byte>>();
                    readBuffer.AddRange(prefixHolder.TryGetPrefix());
                    readBuffer.AddRange(t);
                    tempBuffer = t;
                    receiveOffset += prefixHolder.Count;
                    prefixUsed = true;
                }
                else
                {
                    if (wtf)
                        prefixHolder.HandlePrefix(readBuffer, decodeOffset, receiveOffset - decodeOffset);
                    return false;
                }
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
            if (prefixUsed)
            {
                receiveOffset -= prefixHolder.Count; decodeOffset -= prefixHolder.Count;
                readBuffer = tempBuffer;
                tempBuffer = null;

                prefixHolder.Reset();
            }
            AdjustBuffer();

            return true;
        }

        /// <summary>
        /// This call cleans up the buffer state to make it optimal for next read.
        /// The leading chunks, used by any processed messages, are removed from the front
        ///   of the buffer and added to the back.   Decode and receiver offsets are adjusted accordingly.
        /// If the buffer was grown over the max sustained buffer size (to read a large message) it is shrunken.
        /// </summary>
        private void AdjustBuffer()
        {
            // drop buffers consumed by messages and adjust offsets
            // TODO: This can be optimized further. Linked lists?
            int consumedBytes = 0;
            List < ArraySegment < byte >> segms = new List<ArraySegment<byte>>();
            if (wtf) return;
            while (readBuffer.Count != 0)
            {
                ArraySegment<byte> seg = readBuffer[0];
                if (seg.Count <= decodeOffset - consumedBytes)
                {
                    consumedBytes += seg.Count;
                     segms.Add(seg);
                    readBuffer.Remove(seg);
                    if (!wtf)
                    { 
                        BufferPool.GlobalPool.Release(seg.Array);
                    }
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
                    if (!wtf) readBuffer.AddRange(BufferPool.GlobalPool.GetMultiBuffer(backfillBytes));
                    else
                    {
                       readBuffer.AddRange(segms);
                    }
                    //readBuffer.AddRange(segms); // segms BufferPool.GlobalPool.GetMultiBuffer(backfillBytes)
                }
            }
        }

        private int CalculateKnownMessageSize()
        {
            return headerLength + bodyLength + Message.LENGTH_HEADER_SIZE;
        }

        private List<ArraySegment<byte>> DuplicateBuffer(List<ArraySegment<byte>> body)
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

        private void GrowBuffer()
        {
            //TODO: Add configurable max message size for safety
            //TODO: Review networking layer and add max size checks to all dictionaries, arrays, or other variable sized containers.
            // double buffer size up to max grow block size, then only grow it in those intervals
            int growBlockSize = Math.Min(currentBufferSize, GROW_MAX_BLOCK_SIZE);
            readBuffer.AddRange(BufferPool.GlobalPool.GetMultiBuffer(growBlockSize));
            currentBufferSize += growBlockSize;
        }

        class MessagePrefixHolder
        {
            private List<ArraySegment<byte>> buf;
            public bool HasPrefix { get; set; }
            public int Count { get; set; }

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
                    { //todo : offset
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
                        // buf.Add(new ArraySegment<byte>(segment.Array, bytesStillToSkip, Math.Min(length - countSoFar, segment.Count - bytesStillToSkip)));
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
                var success = false;
                if (HasPrefix)
                {
                    success = true;
                    return buf;
                }

                return null;
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
}
