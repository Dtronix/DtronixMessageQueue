using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc.MessageHandlers;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Rpc {
	public abstract class MessageHandler<TSession, TConfig> 
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Id byte which precedes all messages all messages of this type.
		/// </summary>
		public abstract byte Id { get; } 

		protected TSession Session;

		protected Dictionary<byte, Action<MqMessage>> handlers = new Dictionary<byte, Action<MqMessage>>();

		protected MessageHandler(TSession session) {
			Session = session;
		}

		public void HandleMessage(MqMessage message) {
			if (message[0][0] != Id) {
				Session.Close(SocketCloseReason.ProtocolError);
			}

			// Read the type of message.
			var message_type = message[0].ReadByte(1);

			if (handlers.ContainsKey(message_type)) {
				handlers[message_type].Invoke(message);
			} else {
				// Unknown message type passed.  Disconnect the connection.
				Session.Close(SocketCloseReason.ProtocolError);
			}

		}
	}
}
