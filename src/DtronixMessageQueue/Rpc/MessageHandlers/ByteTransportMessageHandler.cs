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

		private readonly ResponseWait<ByteTransportWaitHandle<TSession, TConfig>> send_operation = new ResponseWait<ByteTransportWaitHandle<TSession, TConfig>>();

		private readonly ResponseWait<ByteTransportWaitHandle<TSession, TConfig>> receive_operation = new ResponseWait<ByteTransportWaitHandle<TSession, TConfig>>();


		public ByteTransportMessageHandler(TSession session) : base(session) {
			Handlers.Add((byte)ByteTransportMessageAction.Request, Request);
			Handlers.Add((byte)ByteTransportMessageAction.Error, Error);
			Handlers.Add((byte)ByteTransportMessageAction.Ready, Ready);
			Handlers.Add((byte)ByteTransportMessageAction.Write, Write);
		}





		private void Error(byte action_handler, MqMessage message) {
			throw new NotImplementedException();
		}

		public void ReadyReceive(ushort transport_id) {
			var ready_frame = Session.CreateFrame(new byte[2], MqFrameType.Last);
			ready_frame.Write(0, transport_id);

			SendHandlerMessage((byte)ByteTransportMessageAction.Ready, ready_frame.ToMessage());
		}

		public ByteTransport<TSession, TConfig> CreateTransport() {
			var handle = send_operation.CreateWaitHandle(null);
			handle.ByteTransport = new ByteTransport<TSession, TConfig>(this, handle.Id, ByteTransport<TSession, TConfig>.Mode.Send);

			var request_frame = Session.CreateFrame(new byte[2], MqFrameType.Last);
			request_frame.Write(0, handle.Id);

			SendHandlerMessage((byte)ByteTransportMessageAction.Request, request_frame.ToMessage());

			return handle.ByteTransport;
		}

		private void Request(byte action_id, MqMessage message) {
			ushort handle_id = message[0].ReadUInt16(0);

			var transport = new ByteTransport<TSession, TConfig>(this, handle_id, ByteTransport<TSession, TConfig>.Mode.Receive);

			var handle = receive_operation.CreateWaitHandle(handle_id);
			handle.ByteTransport = transport;

			SendHandlerMessage((byte)ByteTransportMessageAction.Ready, message);
		}

		private void Ready(byte action_handler, MqMessage message) {
			ushort handle_id = message[0].ReadUInt16(0);

			var transport = (IByteTransport) send_operation[handle_id].ByteTransport;

			if (transport.OnReady() == false) {
				Session.Close(SocketCloseReason.ApplicationError);
			}
		}

		private void Write(byte action_handler, MqMessage message) {
			ushort handle_id = message[0].ReadUInt16(0);

			var transport = (IByteTransport)send_operation[handle_id].ByteTransport;

			transport.OnReceive(message);

			if (transport.OnReady() == false) {
				Session.Close(SocketCloseReason.ApplicationError);
			}

		}



	}
}
