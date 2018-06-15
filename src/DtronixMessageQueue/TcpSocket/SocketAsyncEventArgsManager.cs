using System;
using System.Net.Sockets;
using System.Threading;

namespace DtronixMessageQueue.TcpSocket
{
    /// <summary>
    /// Represents a collection of reusable SocketAsyncEventArgs objects.  
    /// </summary>
    public class SocketAsyncEventArgsManager
    {

        private int _count;

        /// <summary>
        /// The number of SocketAsyncEventArgs instances in use
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Manager which contains the buffers used for all SocketAsyncEventArgs.
        /// </summary>
        private readonly BufferManager _bufferManager;

        /// <summary>
        /// Creates a manager with the specified buffer size and total size for use in the produced objects.
        /// </summary>
        /// <param name="totalBytes">Total size of the buffer for all the sessions.</param>
        /// <param name="bufferSize">Size of the each of the individual buffers for the sessions.</param>
        public SocketAsyncEventArgsManager(int totalBytes, int bufferSize)
        {
            _bufferManager = new BufferManager(totalBytes, bufferSize);
        }

        /// <summary>
        /// Remove the instance from the buffer
        /// </summary>
        /// <param name="eventArgs">SocketAsyncEventArgs containing a buffer user token.</param>
        public void Free(SocketAsyncEventArgs eventArgs)
        {
            Interlocked.Decrement(ref _count);
            _bufferManager.FreeBuffer((ArraySegment<byte>)eventArgs.UserToken);
            eventArgs.Dispose();
        }

        /// <summary>
        /// Creates a SocketAsyncEventArgs instance and adds a buffer to it.
        /// </summary>
        /// <returns></returns>
        public SocketAsyncEventArgs Create()
        {
            Interlocked.Increment(ref _count);
            var eventArg = new SocketAsyncEventArgs();

            try
            {
                var buffer = _bufferManager.GetBuffer();

                // Set the buffer to the event args for freeing later.
                eventArg.UserToken = buffer;

                eventArg.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
            }
            catch
            {
                throw new Exception("Attempted to create more than the max number of sessions.");
            }


            return eventArg;
        }

        public override string ToString()
        {
            return $"Capacity {_count} active objects.";
        }
    }
}