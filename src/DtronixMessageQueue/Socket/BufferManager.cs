using System.Collections.Generic;
using System.Net.Sockets;

namespace DtronixMessageQueue.Socket
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
        }

        /// <summary>
        /// Allocates buffer space used by the buffer pool
        /// </summary>
        public void InitBuffer()
        {
            // create one big large buffer and divide that 
            // out to each SocketAsyncEventArg object
            _buffer = new byte[_numBytes];
        }


        /// <summary>
        /// Assigns a buffer from the buffer pool to the specified SocketAsyncEventArgs object
        /// </summary>
        /// <param name="args"></param>
        /// <returns>true if the buffer was successfully set, else false</returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (_freeIndexPool.Count > 0)
            {
                args.SetBuffer(_buffer, _freeIndexPool.Pop(), _bufferSize);
            }
            else
            {
                if (_numBytes - _bufferSize < _currentIndex)
                {
                    return false;
                }
                args.SetBuffer(_buffer, _currentIndex, _bufferSize);
                _currentIndex += _bufferSize;
            }
            return true;
        }

        /// <summary>
        /// Removes the buffer from a SocketAsyncEventArg object. This frees the buffer back to the buffer pool.
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            _freeIndexPool.Push(args.Offset);
            //args.SetBuffer(null, 0, 0);
        }
    }
}