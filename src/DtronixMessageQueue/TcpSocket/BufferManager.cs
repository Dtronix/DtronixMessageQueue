using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace DtronixMessageQueue.TcpSocket
{
    /// <summary>
    /// Large memory buffer manager
    /// </summary>
    public class BufferManager
    {
        /// <summary>
        /// The total number of bytes controlled by the buffer pool
        /// </summary>
        private readonly int _numBytes;

        /// <summary>
        ///  The underlying byte array maintained by the Buffer Manager
        /// </summary>
        private byte[] _buffer;

        /// <summary>
        /// Stack containing the index of the freed buffers.
        /// </summary>
        private readonly Stack<int> _freeIndexPool;

        /// <summary>
        /// Current index in the buffer pool
        /// </summary>
        private int _currentIndex;

        /// <summary>
        /// Size of each session buffer.
        /// </summary>
        private readonly int _bufferSize;

        /// <summary>
        /// Creates a buffer manager with the specified buffer size and total size.
        /// </summary>
        /// <param name="totalBytes">Total size of the buffer for all the sessions.</param>
        /// <param name="bufferSize">Size of the each of the individual buffers for the sessions.</param>
        public BufferManager(int totalBytes, int bufferSize)
        {
            _numBytes = totalBytes;
            _currentIndex = 0;
            _bufferSize = bufferSize;
            _freeIndexPool = new Stack<int>();
            _buffer = new byte[_numBytes];
        }

        /// <summary>
        /// Returns a buffer from the buffer pool.
        /// </summary>
        /// <returns>ArraySegment with the buffer section</returns>
        public ArraySegment<byte> GetBuffer()
        {
            if (_freeIndexPool.Count > 0)
                return new ArraySegment<byte>(_buffer, _freeIndexPool.Pop(), _bufferSize);

            if (_numBytes - _bufferSize < _currentIndex)
                throw new Exception("Buffer manager exhausted.");

            _currentIndex += _bufferSize;
            return new ArraySegment<byte>(_buffer, _currentIndex, _bufferSize);
        }

        /// <summary>
        /// Removes the buffer from a SocketAsyncEventArg object. This frees the buffer back to the buffer pool.
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(ArraySegment<byte> args)
        {
            _freeIndexPool.Push(args.Offset);
        }
    }
}