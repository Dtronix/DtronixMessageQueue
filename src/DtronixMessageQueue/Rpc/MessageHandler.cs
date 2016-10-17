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

		public delegate void ActionHandler(byte action_handler, MqMessage message);

		/// <summary>
		/// Id byte which precedes all messages all messages of this type.
		/// </summary>
		public abstract byte Id { get; } 

		public TSession Session;

		protected Dictionary<byte, ActionHandler> Handlers = new Dictionary<byte, ActionHandler>();

		protected MessageHandler(TSession session) {
			Session = session;
		}

		public bool HandleMessage(MqMessage message) {
			if (message[0][0] != Id) {
				Session.Close(SocketCloseReason.ProtocolError);
			}

			// Read the type of message.
			var message_type = message[0].ReadByte(1);

			if (Handlers.ContainsKey(message_type)) {
				message.RemoveAt(0);
				Handlers[message_type].Invoke(message_type, message);
				return true;
			}

			// Unknown message type passed.  Disconnect the connection.
			Session.Close(SocketCloseReason.ProtocolError);
			return false;
		}

		public void SendHandlerMessage(byte action_id) {
			SendHandlerMessage(action_id, null);
		}

		public void SendHandlerMessage(byte action_id, MqMessage message) {
			var header_frame = Session.CreateFrame(new byte[2], MqFrameType.More);
			header_frame.Write(0, Id);
			header_frame.Write(1, action_id);

			if (message == null) {
				message = new MqMessage(header_frame);
			} else {
				message.Insert(0, header_frame);
			}

			Session.Send(message);
		}
	}
}
