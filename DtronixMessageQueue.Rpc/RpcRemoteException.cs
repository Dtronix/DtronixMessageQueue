using System;

namespace DtronixMessageQueue.Rpc {
	public class RpcRemoteException {
		public string Message { get; set; }
		public string StackTrace { get; set; }
		public RpcRemoteException(string message) : this(message, null) {
			
		}

		public RpcRemoteException(string message, Exception inner_exception) {
			Message = message;
			StackTrace = inner_exception?.StackTrace;
		}
	}
}
