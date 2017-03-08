using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc.MessageHandlers;

namespace DtronixMessageQueue.Rpc {
	public class ResponseWait<T> : ConcurrentDictionary<ushort, T>
		where T : ResponseWaitHandle, new() {

		/// <summary>
		/// Current call Id which gets incremented for each call return request.
		/// </summary>
		private int id;

		/// <summary>
		/// Lock to increment and loop return ID.
		/// </summary>
		private readonly object id_lock = new object();


		/// <summary>
		/// Creates a waiting operation for this session.  Could be a remote cancellation request or a pending result request.
		/// </summary>
		/// <returns>Wait operation to wait on.</returns>
		public T CreateWaitHandle(ushort? handle_id) {
			var return_wait = new T();

			if (handle_id.HasValue) {
				return_wait.Id = handle_id.Value;
			} else {
				return_wait.ReturnResetEvent = new ManualResetEventSlim();
				// Lock the id incrementation to prevent duplicates.
				lock (id_lock) {
					if (++id > ushort.MaxValue) {
						id = 0;
					}
					return_wait.Id = (ushort)id;
				}
			}



			// Add the wait to the outstanding wait dictionary for retrieval later.
			if (TryAdd(return_wait.Id, return_wait) == false) {
				throw new InvalidOperationException($"Id {return_wait.Id} already exists in the handles dictionary.");
			}

			return return_wait;
		}

		/// <summary>
		/// Called to cancel a operation on this and the recipient connection if specified.
		/// </summary>
		/// <param name="handle_id">Id of the waiting operation to cancel.</param>
		public void Cancel(ushort handle_id) {
			T call_wait_handle;

			// Try to get the wait.  If the Id does not exist, the wait operation has already been completed or removed.
			if (!TryRemove(handle_id, out call_wait_handle)) {
				return;
			}

			call_wait_handle.TokenSource?.Cancel();
		}

		/// <summary>
		/// Called to complete operation on this connection.
		/// </summary>
		/// <param name="handle_id">Id of the waiting operation to complete.</param>
		public T Remove(ushort handle_id) {
			T call_wait_handle;

			// Try to get the wait.  If the Id does not exist, the wait operation has already been completed or removed.
			if (TryRemove(handle_id, out call_wait_handle)) {
				return call_wait_handle;
			}

			return default(T);
		}

	}
}
