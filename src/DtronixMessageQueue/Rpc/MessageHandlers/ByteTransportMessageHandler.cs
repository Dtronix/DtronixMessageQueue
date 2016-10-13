using System;
using System.Collections.Concurrent;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Rpc.MessageHandlers {
	public class ByteTransportMessageHandler<TSession, TConfig> : MessageHandler<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Id byte which precedes all messages all messages of this type.
		/// </summary>
		public override byte Id => 2;

		private readonly ResponseWait<ResponseWaitHandle> send_operation = new ResponseWait<ResponseWaitHandle>();

		private readonly ResponseWait<ResponseWaitHandle> receive_operation = new ResponseWait<ResponseWaitHandle>();

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
			var message_type = (ByteTransportMessageType) message[0].ReadByte(1);

			switch (message_type) {

				case ByteTransportMessageType.RequestTransportHandle:
					ushort handle_id = message[0].ReadUInt16(2);
					long size = message[0].ReadInt64(4);
					ByteTransport<TSession, TConfig> transport;

					if (size > Session.Config.MaxByteTransportLength) {
						var error_frame = Session.CreateFrame(new byte[2], MqFrameType.Last);
						error_frame.Write(0, Id);
						error_frame.Write(1, (byte)ByteTransportMessageType.ResponseTransportHandle);
					}

					try {
						transport = new ByteTransport<TSession, TConfig>(Session, handle_id, size);
					} catch (Exception) {
						
						throw;
					}


					receive_operation.CreateWaitHandle(handle_id);

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
