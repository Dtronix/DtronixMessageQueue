using System;

namespace DtronixMessageQueue.Rpc {
	public class RpcRemoteException : Exception {

		public RpcRemoteException(string message) : base(message) {
			
		}

		public RpcRemoteException(string message, Exception inner_exception) : base(message, inner_exception) {
			
		}
	}
}
