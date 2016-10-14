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
		public sealed override byte Id => 2;

		private readonly ResponseWait<ResponseWaitHandle> send_operation = new ResponseWait<ResponseWaitHandle>();

		private readonly ResponseWait<ResponseWaitHandle> receive_operation = new ResponseWait<ResponseWaitHandle>();

		/// <summary>
		/// Contains all local transports.
		/// </summary>
		public readonly ConcurrentDictionary<ushort, ByteTransport<TSession, TConfig>> RemoteTransports =
			new ConcurrentDictionary<ushort, ByteTransport<TSession, TConfig>>();


		public ByteTransportMessageHandler(TSession session) : base(session) {
			Handlers.Add((byte)ByteTransportMessageAction.RequestTransportHandle, RequestTransportHandle);
		}

		private void RequestTransportHandle(byte action_id, MqMessage message) {
			ushort handle_id = message[0].ReadUInt16(0);
			long size = message[0].ReadInt64(2);
			
			

			if (size > Session.Config.MaxByteTransportLength) {
				var error_frame = Session.CreateFrame(new byte[2], MqFrameType.Last);
				error_frame.Write(0, Id);
				error_frame.Write(1, (byte)ByteTransportMessageAction.ResponseTransportHandle);
			}

			ByteTransport<TSession, TConfig> transport = new ByteTransport<TSession, TConfig>(Session, handle_id, size);


			receive_operation.CreateWaitHandle(handle_id);

			//RemoteTransports.TryAdd(handle_id, remote_transport);

			// Create response.
			var response_frame = Session.CreateFrame(new byte[4], MqFrameType.Last);
			response_frame.Write(0, Id);
			response_frame.Write(1, (byte)ByteTransportMessageAction.ResponseTransportHandle);
			response_frame.Write(2, handle_id);

			Session.Send(response_frame);
		}



	}
}
