using System;
using ProtoBuf;

namespace DtronixMessageQueue.Rpc {
	[ProtoContract]
	public class RpcRemoteExceptionDataContract {
		
		[ProtoMember(1)]
		public string Message { get; set; }

		[ProtoMember(2)]
		public string ExceptionType { get; set; }

		public RpcRemoteExceptionDataContract() {
			
		}
		public RpcRemoteExceptionDataContract(string message, Exception exception) {
			ExceptionType = exception.GetType().Name;
			Message = message;
		}

		public RpcRemoteExceptionDataContract(Exception exception) {
			ExceptionType = exception.GetType().Name;
			Message = exception.Message;
		}
	}
}
