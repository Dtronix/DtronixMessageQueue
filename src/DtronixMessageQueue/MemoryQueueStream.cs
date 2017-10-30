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
    public class MemoryQueueStream : Stream
    {

        public override bool CanSeek => false;

        /// <summary>
        /// Always returns 0
        /// </summary>
        public override long Position {
            get => 0;
            set => throw new NotSupportedException(nameof(MemoryQueueStream) + " is not seekable");
        }

        public override bool CanWrite => true;


        public override bool CanRead => true;

        private long _length;

        public override long Length => _length;

        private int _chunkReadIndex;

        //Maintains the streams data.  The Queue object provides an easy and efficient way to add and remove data
        //Each item in the queue represents each write to the stream.  Every call to write translates to an item in the queue
        internal readonly Queue<byte[]> Buffer;

        public MemoryQueueStream()
        {
            Buffer = new Queue<byte[]>();

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
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            //We don't want to use the buffer passed in, as it could be altered by the caller
            var copyBuff = new byte[count];
            System.Buffer.BlockCopy(buffer, offset, copyBuff, 0, count);

            _length += count;

            //Add the data to the queue
            Buffer.Enqueue(copyBuff);
        }

        /// <summary>
        /// Adds the passed buffer to the steam.
        /// </summary>
        /// <param name="buffer">Data to add to the stream</param>
        public void Write(byte[] buffer)
        {
            //Add the data to the queue
            Buffer.Enqueue(buffer);

            _length += buffer.Length;
        }

       
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(GetType().Name + " is not seekable");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(GetType().Name + " length can not be changed");
        }

        public override void Flush()
        {
        }
    }
}
