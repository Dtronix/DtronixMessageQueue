using System.Collections.Concurrent;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Rpc.MessageHandlers {
	public class ByteTransportMessageHandler<TSession, TConfig> : MessageHandler<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Current call Id wich gets incremented for each call return request.
		/// </summary>
		private int transport_id;

		/// <summary>
		/// Lock to increment and loop return ID.
		/// </summary>
		private readonly object transport_id_lock = new object();

		/// <summary>
		/// Id byte which precedes all messages all messages of this type.
		/// </summary>
		public override byte Id => 2;

		/// <summary>
		/// Contains all local transports.
		/// </summary>
		public readonly ConcurrentDictionary<ushort, ByteTransport<TSession, TConfig>> RemoteTransports =
			new ConcurrentDictionary<ushort, ByteTransport<TSession, TConfig>>();


		public ByteTransportMessageHandler(TSession session) : base(session) {
		}

		

		public override bool HandleMessage(MqMessage message) {
			if (message[0][0] != Id) {
				return false;
			}

			// Read the type of message.
			var message_type = (ByteTransportMessageType)message[0].ReadByte(1);

			switch (message_type) {

				case ByteTransportMessageType.RequestTransportHandle:
					ushort handle_id;

					lock (transport_id_lock) {
						if (++transport_id > ushort.MaxValue) {
							transport_id = 1;
						}
						handle_id = (ushort)transport_id;
					}
					var remote_transport = new ByteTransport<TSession, TConfig>(Session);

					RemoteTransports.TryAdd(handle_id, remote_transport);

					// Create response.
					var response_frame = Session.CreateFrame(new byte[4], MqFrameType.Last);
					response_frame.Write(0, Id);
					response_frame.Write(1, (byte)ByteTransportMessageType.ResponseTransportHandle);
					response_frame.Write(2, handle_id);

					Session.Send(response_frame);

					break;

				case ByteTransportMessageType.ResponseTransportHandle:
					var id = message[0].ReadUInt16(2);

					//message_writer.Write((byte)ByteTransportMessageType.Write);
					break;

				default:
					// Unknown message type passed.  Disconnect the connection.
					Session.Close(SocketCloseReason.ProtocolError);
					break;
			}

			return true;

		}
	}
}
