using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	/// <summary>
	/// Represents a collection of reusable SocketAsyncEventArgs objects.  
	/// </summary>
	public class SocketAsyncEventArgsPool {
		Stack<SocketAsyncEventArgs> pool;

		/// <summary>
		/// Initializes the object pool to the specified size
		/// </summary>
		/// <param name="capacity">The "capacity" parameter is the maximum number of SocketAsyncEventArgs objects the pool can hold</param>
		public SocketAsyncEventArgsPool(int capacity) {
			pool = new Stack<SocketAsyncEventArgs>(capacity);
		}

		/// <summary>
		/// Add a SocketAsyncEventArg instance to the pool
		/// </summary>
		/// <param name="item">The "item" parameter is the SocketAsyncEventArgs instance to add to the pool</param>
		public void Push(SocketAsyncEventArgs item) {
			if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
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

		/// <summary>
		/// The number of SocketAsyncEventArgs instances in the pool
		/// </summary>
		public int Count {
			get { return pool.Count; }
		}

	}
}
