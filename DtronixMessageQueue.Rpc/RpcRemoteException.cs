using System;
using ProtoBuf;

namespace DtronixMessageQueue.Rpc {
	[ProtoContract]
	public class RpcRemoteException {
		
		[ProtoMember(1)]
		public string Message { get; set; }

		[ProtoMember(2)]
		public string ExceptionType { get; set; }

		public RpcRemoteException() {
			
		}
		public RpcRemoteException(string message, Exception exception) {
			ExceptionType = exception.GetType().Name;
			Message = message;
		}

		public RpcRemoteException(Exception exception) {
			ExceptionType = exception.GetType().Name;
			Message = exception.Message;
		}
	}
}
