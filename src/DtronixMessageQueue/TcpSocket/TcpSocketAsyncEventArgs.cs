using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace DtronixMessageQueue.TcpSocket
{
    public class TcpSocketAsyncEventArgs : SocketAsyncEventArgs
    {
        private IMemoryOwner<byte> _memoryOwner;
        
        private int _currentWritePosition = 0;


        public TcpSocketAsyncEventArgs(BufferMemoryPool argsBufferPool)
        {
            _memoryOwner = argsBufferPool.Rent();

            SetBuffer(_memoryOwner.Memory);
        }

        public int Write(ReadOnlyMemory<byte> source)
        {
            if(_currentWritePosition == -1)
                throw new InvalidOperationException("Args are set for sending but has not been sent.");

            var destination = _memoryOwner.Memory;
            var remaining = destination.Length - _currentWritePosition;
            

            // Check to see if the source buffer is smaller than the remaining available.
            if (source.Length <= remaining)
            {
                // If we do do not have any data written so far, just copy the
                // entire source to the destination.  If we already have data written,
                // slice the destination array.
                source.CopyTo(_currentWritePosition == 0
                    ? destination
                    : destination.Slice(remaining, source.Length));

                _currentWritePosition += source.Length;
                return 0;
            }

            // The provided source buffer is too large for the current state of the buffer.
            // Write what can be written and then return what is remaining.

            source.CopyTo(destination.Slice(_currentWritePosition, remaining));

            // Set write position to the end.
            _currentWritePosition = destination.Length;

            return source.Length - remaining;
        }

        /// <summary>
        /// Prepares the current event args for sending of data.
        /// Data can not be written to the buffer until ResetSend() is called.
        /// </summary>
        public void PrepareForSend()
        {
            SetBuffer(_memoryOwner.Memory.Slice(0, _currentWritePosition));
            _currentWritePosition = -1;
        }

        /// <summary>
        /// Resets the state of the socket for more writing.
        /// </summary>
        public void ResetSend()
        {
            _currentWritePosition = 0;
        }

        public void Free()
        {
            if (_memoryOwner == null)
                throw new ObjectDisposedException(nameof(TcpSocketAsyncEventArgs));

            _memoryOwner.Dispose();
            _memoryOwner = null;
            Dispose();
        }

    }
}