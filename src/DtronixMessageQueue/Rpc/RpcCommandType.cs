using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc {
	/// <summary>
	/// Command types for Rpc commands.
	/// </summary>
	public enum RpcCommandType : byte {
		/// <summary>
		/// Server is sending a welcome message to the client with basic information about the server
		/// and authentication requirements.
		/// </summary>
		WelcomeMessage = 0,

		/// <summary>
		/// Client is sending an authentication request to the server with token data.
		/// </summary>
		AuthenticationRequest = 1,

		/// <summary>
		/// Server is sending the result of the authentication request.
		/// </summary>
		AuthenticationResult = 2,



		/// <summary>
		/// Sends a request to the client/server session for a stream handle to be created to write to.
		/// </summary>
		RequestStreamHandle = 10,

		/// <summary>
		/// Sends a request to the client/server session for a stream handle be closed.
		/// </summary>
		CloseStreamHandle = 11,
	}
}
