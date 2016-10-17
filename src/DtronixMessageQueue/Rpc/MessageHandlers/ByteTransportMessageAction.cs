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
		Request = 1,

		/// <summary>
		/// Sent when the transport handle is in an error state.
		/// </summary>
		Error = 2,

		/// <summary>
		/// Lets the recipient session know it is ok to send the next packet.
		/// </summary>
		Ready = 3,

		/// <summary>
		/// Sends a request to the client/server session for a stream handle be closed.
		/// </summary>
		Close = 4,

		/// <summary>
		/// This message contains the byte buffer.
		/// </summary>
		Write = 5
		
	}


}
