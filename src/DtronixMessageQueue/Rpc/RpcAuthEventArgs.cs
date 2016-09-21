using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Authentication event arguments for verifying the authenticity of a session.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public class RpcAuthenticateEventArgs<TSession, TConfig> : EventArgs
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Connected session.
		/// </summary>
		public RpcSession<TSession, TConfig> Session { get; }

		/// <summary>
		/// Authentication frame to be passed to the server for verification.
		/// </summary>
		public byte[] AuthData { get; set; }

		/// <summary>
		/// (Server)
		/// Set to true if the authentication data passed is valid; False otherwise.
		/// </summary>
		public bool Authenticated { get; set; }

		/// <summary>
		/// Creates new authentication arguments for this authentication event.
		/// </summary>
		/// <param name="session">Session type for this connection.</param>
		public RpcAuthenticateEventArgs(RpcSession<TSession, TConfig> session) {
			Session = session;
		}
	}
}
