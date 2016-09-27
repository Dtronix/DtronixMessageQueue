using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc {
	public abstract class MessageHandler<TSession, TConfig> 
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Id byte which precedes all messages all messages of this type.
		/// </summary>
		public abstract byte Id { get; } 

		protected TSession Session;

		protected MessageHandler(TSession session) {
			Session = session;
		}

		public abstract bool HandleMessage(MqMessage message);
	}
}
