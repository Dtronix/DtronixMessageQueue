using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc {
	public class RpcByteTransport<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		private RpcSession<TSession, TConfig> session;

		private MqMessageReader reader;
		private MqMessageWriter writer;


		/// <summary>
		/// Create a RpcStream in writer mode.
		/// </summary>
		/// <param name="session">Session to write to.</param>
		public RpcByteTransport(RpcSession<TSession, TConfig> session) {
			this.session = session;
			writer = new MqMessageWriter(this.session.Config);
		}

		/// <summary>
		/// Create a RpcStream in reader mode.
		/// </summary>
		/// <param name="session">Session to write to.</param>
		public RpcByteTransport(RpcSession<TSession, TConfig> session, ushort id) {
			this.session = session;
			writer = new MqMessageWriter(this.session.Config);
		}


	}
}
