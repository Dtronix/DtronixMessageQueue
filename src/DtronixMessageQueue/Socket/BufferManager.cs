using System.Collections.Generic;
using System.Net.Sockets;

namespace DtronixMessageQueue.Socket {
	/// <summary>
	/// Large memory buffer manager
	/// </summary>
	public class BufferManager {

		/// <summary>
		/// The total number of bytes controlled by the buffer pool
		/// </summary>
		private readonly int num_bytes;

		/// <summary>
		///  The underlying byte array maintained by the Buffer Manager
		/// </summary>
		private byte[] buffer;

		/// <summary>
		/// Stack containing the index of the freed buffers.
		/// </summary>
		private readonly Stack<int> free_index_pool;

		/// <summary>
		/// Current index in the buffer pool
		/// </summary>
		private int current_index;

		/// <summary>
		/// Size of each session buffer.
		/// </summary>
		private readonly int buffer_size;

		/// <summary>
		/// Creates a buffer manager with the specified buffer size and total size.
		/// </summary>
		/// <param name="total_bytes">Total size of the buffer for all the sessions.</param>
		/// <param name="buffer_size">Size of the each of the individual buffers for the sessions.</param>
		public BufferManager(int total_bytes, int buffer_size) {
			num_bytes = total_bytes;
			current_index = 0;
			this.buffer_size = buffer_size;
			free_index_pool = new Stack<int>();
		}

		/// <summary>
		/// Allocates buffer space used by the buffer pool
		/// </summary>
		public void InitBuffer() {
			// create one big large buffer and divide that 
			// out to each SocketAsyncEventArg object
			buffer = new byte[num_bytes];
		}


		/// <summary>
		/// Assigns a buffer from the buffer pool to the specified SocketAsyncEventArgs object
		/// </summary>
		/// <param name="args"></param>
		/// <returns>true if the buffer was successfully set, else false</returns>
		public bool SetBuffer(SocketAsyncEventArgs args) {
			if (free_index_pool.Count > 0) {
				args.SetBuffer(buffer, free_index_pool.Pop(), buffer_size);
			} else {
				if (num_bytes - buffer_size < current_index) {
					return false;
				}
				args.SetBuffer(buffer, current_index, buffer_size);
				current_index += buffer_size;
			}
			return true;
		}

		/// <summary>
		/// Removes the buffer from a SocketAsyncEventArg object. This frees the buffer back to the buffer pool.
		/// </summary>
		/// <param name="args"></param>
		public void FreeBuffer(SocketAsyncEventArgs args) {
			free_index_pool.Push(args.Offset);
			args.SetBuffer(null, 0, 0);
		}

	}
}
