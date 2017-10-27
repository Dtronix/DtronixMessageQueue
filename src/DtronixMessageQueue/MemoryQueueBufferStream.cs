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
    public class MemoryQueueBufferStream : Stream
    {

        public override bool CanSeek => false;

        /// <summary>
        /// Always returns 0
        /// </summary>
        public override long Position {
            get => 0;
            set => throw new NotSupportedException(GetType().Name + " is not seekable");
        }

        public override bool CanWrite => true;


        public override bool CanRead => true;



        public override long Length {
            get {

                if (_buffer == null)
                {
                    return 0;
                }

                if (_buffer.Count == 0)
                {
                    return 0;
                }

                return _buffer.Sum(b => b.Data.Length - b.ChunkReadStartIndex);
            }
        }

        /// <summary>
        /// Represents a single write into the MemoryQueueBufferStream.  Each write is a separate chunk
        /// </summary>
        private class Chunk
        {
            /// <summary>
            /// As we read through the chunk, the start index will increment.  When we get to the end of the chunk,
            /// we will remove the chunk
            /// </summary>
            public int ChunkReadStartIndex { get; set; }

            /// <summary>
            /// Actual Data
            /// </summary>
            public byte[] Data { get; set; }
        }

        //Maintains the streams data.  The Queue object provides an easy and efficient way to add and remove data
        //Each item in the queue represents each write to the stream.  Every call to write translates to an item in the queue
        private readonly Queue<Chunk> _buffer;

        public MemoryQueueBufferStream()
        {
            _buffer = new Queue<Chunk>();
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
            while (totalBytesRead <= count && _buffer.Count > 0)
            {
                //Get first chunk from the queue
                var chunk = _buffer.Peek();

                //Determine how much of the chunk there is left to read
                var unreadChunkLength = chunk.Data.Length - chunk.ChunkReadStartIndex;

                //Determine how much of the unread part of the chunk we can actually read
                var bytesToRead = Math.Min(unreadChunkLength, remainingBytesToRead);

                if (bytesToRead > 0)
                {
                    //Read from the chunk into the buffer
                    Buffer.BlockCopy(chunk.Data, chunk.ChunkReadStartIndex, buffer, offset + totalBytesRead, bytesToRead);

                    totalBytesRead += bytesToRead;
                    remainingBytesToRead -= bytesToRead;

                    //If the entire chunk has been read,  remove it
                    if (chunk.ChunkReadStartIndex + bytesToRead >= chunk.Data.Length)
                    {
                        _buffer.Dequeue();
                    }
                    else
                    {
                        //Otherwise just update the chunk read start index, so we know where to start reading on the next call
                        chunk.ChunkReadStartIndex = chunk.ChunkReadStartIndex + bytesToRead;
                    }
                }
                else
                {
                    break;
                }
            }

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
            var bufSave = new byte[count];
            Buffer.BlockCopy(buffer, offset, bufSave, 0, count);

            //Add the data to the queue
            _buffer.Enqueue(new Chunk { ChunkReadStartIndex = 0, Data = bufSave });
        }

        /// <summary>
        /// Adds the passed buffer to the steam.
        /// </summary>
        /// <param name="buffer">Data to add to the stream</param>
        public void Write(byte[] buffer)
        {
            //Add the data to the queue
            _buffer.Enqueue(new Chunk { ChunkReadStartIndex = 0, Data = buffer });
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
