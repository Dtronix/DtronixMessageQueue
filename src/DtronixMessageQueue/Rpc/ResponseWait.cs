using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc.MessageHandlers;

namespace DtronixMessageQueue.Rpc {
	public class ResponseWait<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {
		private readonly byte handler_id;
		private readonly TSession session;


		/// <summary>
		/// Current call Id wich gets incremented for each call return request.
		/// </summary>
		private int id;

		/// <summary>
		/// Lock to increment and loop return ID.
		/// </summary>
		private readonly object id_lock = new object();


		/// <summary>
		/// Contains all outstanding call returns pending a return of data from the recipient connection.
		/// </summary>
		private readonly ConcurrentDictionary<ushort, ResponseWaitHandle> remote_wait_handles =
			new ConcurrentDictionary<ushort, ResponseWaitHandle>();

		/// <summary>
		/// Contains all operations running on this session which are cancellable.
		/// </summary>
		private readonly ConcurrentDictionary<ushort, ResponseWaitHandle> local_wait_handles =
			new ConcurrentDictionary<ushort, ResponseWaitHandle>();


		public ResponseWait(byte handler_id, TSession session) {
			this.handler_id = handler_id;
			this.session = session;
		}


		/// <summary>
		/// Creates a waiting operation for this session.  Could be a remote cancellation request or a pending result request.
		/// </summary>
		/// <returns>Wait operation to wait on.</returns>
		public ResponseWaitHandle CreateLocalWaitHandle() {
			var return_wait = new ResponseWaitHandle {
				ReturnResetEvent = new ManualResetEventSlim()
			};

			// Lock the id incrementation to prevent duplicates.
			lock (id_lock) {
				if (++id > ushort.MaxValue) {
					id = 0;
				}
				return_wait.Id = (ushort)id;
			}

			// Add the wait to the outstanding wait dictionary for retrieval later.
			if (local_wait_handles.TryAdd(return_wait.Id, return_wait) == false) {
				throw new InvalidOperationException($"Id {return_wait.Id} already exists in the local handles dictionary.");
			}

			return return_wait;
		}

		public ResponseWaitHandle CreateRemoteWaitHandle(ushort id) {
			var return_wait = new ResponseWaitHandle {
				Id = id
			};

			// Add the wait to the outstanding wait dictionary for retrieval later.
			if (remote_wait_handles.TryAdd(return_wait.Id, return_wait) == false) {
				throw new InvalidOperationException($"Id {return_wait.Id} already exists in the remote handles dictionary.");
			}

			return return_wait;
		}


		/// <summary>
		/// Called to cancel a localally called operation on this and the recipient connection.
		/// </summary>
		/// <param name="id">Id of the waiting operation to cancel.</param>
		/// <param name="remote_cancel">True if this wait should notify the recipient.</param>
		public void LocalCancel(ushort id, bool remote_cancel) {
			ResponseWaitHandle call_wait_handle;

			// Try to get the wait.  If the Id does not exist, the wait operation has already been completed or removed.
			if (!local_wait_handles.TryRemove(id, out call_wait_handle)) {
				return;
			}

			if (!remote_cancel) {
				return;
			}

			var frame = new MqFrame(new byte[4], MqFrameType.Last, session.Config);
			frame.Write(0, handler_id);
			frame.Write(1, (byte) RpcCallMessageType.MethodCancel);
			frame.Write(2, id);

			session.Send(frame);
		}

		/// <summary>
		/// Called to complete a localally called operation on this and the recipient connection.
		/// </summary>
		/// <param name="id">Id of the waiting operation to complete.</param>
		public void LocalCompete(ushort id) {
			ResponseWaitHandle call_wait_handle;

			// Try to get the wait.  If the Id does not exist, the wait operation has already been completed or removed.
			local_wait_handles.TryRemove(id, out call_wait_handle);
		}

		/// <summary>
		/// Gets the local response wait handle
		/// </summary>
		/// <param name="id">Id of the waiting operation to get.</param>
		public ResponseWaitHandle LocalGet(ushort id) {
			ResponseWaitHandle call_wait_handle;

			// Try to get the wait.  If the Id does not exist, the wait operation has already been completed or removed.
			local_wait_handles.TryGetValue(id, out call_wait_handle);

			return call_wait_handle;
		}



		/// <summary>
		/// Called to cancel a remotely called operation on this session.
		/// </summary>
		/// <param name="id">Id of the waiting operation to cancel.</param>
		public void RemoteCancel(ushort id) {
			ResponseWaitHandle call_wait_handle;

			// Try to get the wait.  If the Id does not exist, the wait operation has already been completed or removed.
			if (remote_wait_handles.TryRemove(id, out call_wait_handle)) {
				call_wait_handle.TokenSource?.Cancel();
			}
		}

		/// <summary>
		/// Called to complete a remotely called operation on this session.
		/// </summary>
		/// <param name="id">Id of the waiting operation to cancel.</param>
		public void RemoteComplete(ushort id) {
			ResponseWaitHandle call_wait_handle;

			// Try to get the wait.  If the Id does not exist, the wait operation has already been completed or removed.
			remote_wait_handles.TryRemove(id, out call_wait_handle);
		}

	}
}
