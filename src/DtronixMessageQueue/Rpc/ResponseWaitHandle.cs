using System;
using System.Threading;

namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Class which represents a task which is waiting on another event to occur.
	/// </summary>
	public class ResponseWaitHandle {

		/// <summary>
		/// Id of this wait operation.  Used to coordinate between client/server.
		/// </summary>
		public ushort Id { get; set; }
		
		/// <summary>
		/// Reset event used to hold the requesting 
		/// </summary>
		public ManualResetEventSlim ReturnResetEvent { get; set; }

		/// <summary>
		/// Message that the other session receives from the connection that is associated with the return value for this request.
		/// </summary>
		public MqMessage ReturnMessage { get; set; }

		/// <summary>
		/// Cancellation token for the request.
		/// </summary>
		public CancellationToken Token { get; }

		/// <summary>
		/// Cancellation token source for the request.
		/// </summary>
		private CancellationTokenSource token_source;

		/// <summary>
		/// Contains the time that this call wait was created to check for timeouts.
		/// </summary>
		public DateTime Created { get; } = DateTime.UtcNow;

		public ResponseWaitHandle() : this(new CancellationTokenSource()) {
		}

		public ResponseWaitHandle(CancellationTokenSource cancellation_token_source) {
			token_source = cancellation_token_source;
			Token = cancellation_token_source.Token;
		}

		public void Cancel() {
			token_source.Cancel();
		}
	}
}
