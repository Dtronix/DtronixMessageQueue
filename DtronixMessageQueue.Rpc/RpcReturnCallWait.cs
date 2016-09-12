using System;
using System.Threading;

namespace DtronixMessageQueue.Rpc {
	public class RpcReturnCallWait {
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
		public CancellationToken Token { get; set; }

		/// <summary>
		/// Contains the time that this call wait was created to check for timeouts.
		/// </summary>
		public DateTime Created { get; } = DateTime.UtcNow;
	}
}
