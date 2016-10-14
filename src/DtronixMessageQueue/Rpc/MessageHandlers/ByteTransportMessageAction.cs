namespace DtronixMessageQueue.Rpc.MessageHandlers {


	/// <summary>
	/// Type of message which is being sent.
	/// </summary>
	public enum ByteTransportMessageAction : byte {

		/// <summary>
		/// Unknown default type.
		/// </summary>
		Unset = 0,

		/// <summary>
		/// Sends a request to the client/server session for a stream handle to be created to write to.
		/// </summary>
		RequestTransportHandle = 1,

		/// <summary>
		/// Sends a request to the client/server session for a stream handle to be created to write to.
		/// </summary>
		ResponseTransportHandle = 2,

		/// <summary>
		/// Sends a request to the client/server session for a stream handle be closed.
		/// </summary>
		CloseTransportHandle = 3,

		/// <summary>
		/// Sends data to the client/server.
		/// </summary>
		Write = 4,
	}


}
