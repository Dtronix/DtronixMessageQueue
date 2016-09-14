using System;
using ProtoBuf;

namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Class containing the information about a remote exception occuring.
	/// </summary>
	[ProtoContract]
	public class RpcRemoteExceptionDataContract {
		
		/// <summary>
		/// Remote exception message.
		/// </summary>
		[ProtoMember(1)]
		public string Message { get; set; }

		/// <summary>
		/// Type of the remote exception.
		/// </summary>
		[ProtoMember(2)]
		public string ExceptionType { get; set; }

		/// <summary>
		/// Creates instance of a default exception.
		/// </summary>
		public RpcRemoteExceptionDataContract() {
			
		}


		/// <summary>
		/// Creates instance of an exception with a custom message.
		/// </summary>
		/// <param name="message">Message of the exception.</param>
		/// <param name="exception">Exception which occurred.</param>
		public RpcRemoteExceptionDataContract(string message, Exception exception) {
			ExceptionType = exception.GetType().Name;
			Message = message;
		}

		/// <summary>
		/// Creates instance of an exception with the specified base exception
		/// </summary>
		/// <param name="exception">Exception which occurred.</param>
		public RpcRemoteExceptionDataContract(Exception exception) {
			ExceptionType = exception.GetType().Name;
			Message = exception.Message;
		}
	}
}
