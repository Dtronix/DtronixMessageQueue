namespace DtronixMessageQueue.Rpc.MessageHandlers {

	/// <summary>
	/// Type of message which is being sent.
	/// </summary>
	public enum RpcCallMessageType : byte {

		/// <summary>
		/// Unknown default type.
		/// </summary>
		Unset = 0,

		/// <summary>
		/// Message is a standard Rpc call with a return value.
		/// </summary>
		MethodCall = 1,

		/// <summary>
		/// Message is a Rpc call with no return value.
		/// </summary>
		MethodCallNoReturn = 2,

		/// <summary>
		/// Message is a Rpc response with a return value.
		/// </summary>
		MethodReturn = 3,


		/// <summary>
		/// Message is a Rpc response.  Message contains information about the exception thrown.
		/// </summary>
		MethodException = 4,

		/// <summary>
		/// Message used to cancel a pending operation.
		/// </summary>
		MethodCancel = 5,
	}
}
