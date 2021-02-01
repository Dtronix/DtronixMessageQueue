using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue
{
    /// <summary>
    /// This stream maintains data only until the data is read, then it is purged from the stream.
    /// </summary>
    public class MemoryQueueStream
    {
        private long _length;

        public long Length => _length;

        private int _chunkReadIndex;

        //Maintains the streams data.  The Queue object provides an easy and efficient way to add and remove data
        //Each item in the queue represents each write to the stream.  Every call to write translates to an item in the queue
        internal readonly Queue<Memory<byte>> Buffer;

        public MemoryQueueStream()
        {
            Buffer = new Queue<Memory<byte>>();

            _length = 0;
            _chunkReadIndex = 0;
        }

        /// <summary>
        /// Reads up to count bytes from the stream, and removes the read data from the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var remainingBytesToRead = count;
            var totalBytesRead = 0;

            //Read until we hit the requested count, or until we hav nothing left to read
            while (totalBytesRead <= count && Buffer.Count > 0)
            {
                //Get first chunk from the queue
                var chunk = Buffer.Peek();

                //Determine how much of the chunk there is left to read
                var unreadChunkLength = chunk.Length - _chunkReadIndex;

                //Determine how much of the unread part of the chunk we can actually read
                var bytesToRead = Math.Min(unreadChunkLength, remainingBytesToRead);

                if (bytesToRead > 0)
                {
                    //Read from the chunk into the buffer
                    System.Buffer.BlockCopy(chunk, _chunkReadIndex, buffer, offset + totalBytesRead, bytesToRead);

                    totalBytesRead += bytesToRead;
                    remainingBytesToRead -= bytesToRead;

                    //If the entire chunk has been read,  remove it
                    if (_chunkReadIndex + bytesToRead >= chunk.Length)
                    {
                        Buffer.Dequeue();
                        _chunkReadIndex = 0;
                    }
                    else
                    {
                        //Otherwise just update the chunk read start index, so we know where to start reading on the next call
                        _chunkReadIndex += bytesToRead;
                    }
                }
                else
                {
                    break;
                }
            }

            _length -= totalBytesRead;
            return totalBytesRead;
        }

        /// <summary>
        /// Writes data to the stream
        /// </summary>
        /// <param name="buffer">Data to copy into the stream</param>
        public void Write(Memory<byte> buffer)
        {
            //Add the data to the queue
            Buffer.Enqueue(buffer);
        }
    }
}
