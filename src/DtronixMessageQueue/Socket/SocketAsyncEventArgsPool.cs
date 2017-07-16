using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace DtronixMessageQueue.Socket
{
    /// <summary>
    /// Represents a collection of reusable SocketAsyncEventArgs objects.  
    /// </summary>
    public class SocketAsyncEventArgsPool
    {
        /// <summary>
        /// Total capacity of the pool.
        /// </summary>
        private readonly int _capacity;

        private readonly BufferManager _bufferManager;

        /// <summary>
        /// Pre-configured stack of event args to use.
        /// </summary>
        private readonly Stack<SocketAsyncEventArgs> _pool;


        /// <summary>
        /// The number of SocketAsyncEventArgs instances in the pool
        /// </summary>
        public int Count => _pool.Count;

        /// <summary>
        /// Initializes the object pool to the specified size
        /// </summary>
        /// <param name="capacity">The "capacity" parameter is the maximum number of SocketAsyncEventArgs objects the pool can hold</param>
        /// <param name="bufferManager"></param>
        public SocketAsyncEventArgsPool(int capacity, BufferManager bufferManager)
        {
            _capacity = capacity;
            _bufferManager = bufferManager;

            _pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// <summary>
        /// Add a SocketAsyncEventArg instance to the pool
        /// </summary>
        /// <param name="item">The "item" parameter is the SocketAsyncEventArgs instance to add to the pool</param>
        public void Push(SocketAsyncEventArgs item)
        {
            _bufferManager.FreeBuffer(item);
            item.Dispose();

        }

        /// <summary>
        /// Removes a SocketAsyncEventArgs instance from the pool and returns the object removed from the pool
        /// </summary>
        /// <returns></returns>
        public SocketAsyncEventArgs Pop()
        {

            var eventArg = new SocketAsyncEventArgs();

            _bufferManager.SetBuffer(eventArg);

            return eventArg;
        }

        public override string ToString()
        {
            return $"Capacity ({_pool.Count}/{_capacity})";
        }
    }
}