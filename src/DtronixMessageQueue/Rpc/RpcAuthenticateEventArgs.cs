using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc {
	public class RpcAuthenticateEventArgs<TSession, TConfig> : EventArgs
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Connected session.
		/// </summary>
		public TSession Session { get; }

		/// <summary>
		/// Authentication frame to be passed to the server for verification.
		/// </summary>
		public byte[] AuthData { get; set; }

		/// <summary>
		/// (Server)
		/// Set to true if the authentication data passed is valid; False otherwise.
		/// </summary>
		public bool Authenticated { get; set; }

		public RpcAuthenticateEventArgs(TSession session) {
			Session = session;
		}
	}
}
