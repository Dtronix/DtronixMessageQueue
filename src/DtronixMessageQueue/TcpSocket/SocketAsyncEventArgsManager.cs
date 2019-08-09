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
        private readonly BufferMemoryPool _bufferManager;

        /// <summary>
        /// Creates a manager with the specified buffer size and total size for use in the produced objects.
        /// </summary>
        /// <param name="rentBufferSize">Size for the buffer of each arg</param>
        /// <param name="maxRentals">Max number of available sockets.</param>
        public SocketAsyncEventArgsManager(int rentBufferSize, int maxRentals)
        {
            _bufferManager = new BufferMemoryPool(rentBufferSize, maxRentals);
        }

        /// <summary>
        /// Creates a SocketAsyncEventArgs instance and adds a buffer to it.
        /// </summary>
        /// <returns></returns>
        public SocketAsyncEventArgs Create()
        {
            Interlocked.Increment(ref _count);
            var eventArg = new TcpSocketAsyncEventArgs();

            try
            {
                var buffer = _bufferManager.Rent();

                // Set the buffer to the event args for freeing later.
                eventArg.MemoryOwner = buffer;

                eventArg.SetBuffer(buffer.Memory);
            }
            catch
            {
                throw new Exception("Attempted to create more than the max number of sessions.");
            }


            return eventArg;
        }

        public override string ToString()
        {
            return $"Capacity {_bufferManager.TotalRentals} of {_bufferManager.TotalRentals} active objects.";
        }
    }
}