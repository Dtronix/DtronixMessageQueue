using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace DtronixMessageQueue.Socket {
	/// <summary>
	/// Represents a collection of reusable SocketAsyncEventArgs objects.  
	/// </summary>
	public class SocketAsyncEventArgsPool {

		/// <summary>
		/// Total capacity of the pool.
		/// </summary>
		private readonly int capacity;

		/// <summary>
		/// Preconfigured stack of event args to use.
		/// </summary>
		private readonly Stack<SocketAsyncEventArgs> pool;


		/// <summary>
		/// The number of SocketAsyncEventArgs instances in the pool
		/// </summary>
		public int Count => pool.Count;

		/// <summary>
		/// Initializes the object pool to the specified size
		/// </summary>
		/// <param name="capacity">The "capacity" parameter is the maximum number of SocketAsyncEventArgs objects the pool can hold</param>
		public SocketAsyncEventArgsPool(int capacity) {
			this.capacity = capacity;

			pool = new Stack<SocketAsyncEventArgs>(capacity);
		}

		/// <summary>
		/// Add a SocketAsyncEventArg instance to the pool
		/// </summary>
		/// <param name="item">The "item" parameter is the SocketAsyncEventArgs instance to add to the pool</param>
		public void Push(SocketAsyncEventArgs item) {
			if (item == null) {
				throw new ArgumentNullException(nameof(item), "Items added to a SocketAsyncEventArgsPool cannot be null");
			}
			lock (pool) {
				pool.Push(item);
			}
		}

		/// <summary>
		/// Removes a SocketAsyncEventArgs instance from the pool and returns the object removed from the pool
		/// </summary>
		/// <returns></returns>
		public SocketAsyncEventArgs Pop() {
			lock (pool) {
				return pool.Pop();
			}
		}

		public override string ToString() {
			return $"Capacity ({pool.Count}/{capacity})";
		}
	}
}
