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
		public MqMessage Message { get; set; }

		/// <summary>
		/// Byte if of the returned message.
		/// </summary>
		public byte MessageActionId { get; set; }

		/// <summary>
		/// Cancellation token for the request.
		/// </summary>
		public CancellationToken Token { get; set; }

		/// <summary>
		/// Cancellation token source for the request.
		/// </summary>
		public CancellationTokenSource TokenSource { get; set; }

		/// <summary>
		/// Contains the time that this call wait was created to check for timeouts.
		/// </summary>
		public DateTime Created { get; } = DateTime.UtcNow;

	}
}
