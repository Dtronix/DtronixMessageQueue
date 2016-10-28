using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Rpc.MessageHandlers {
	public class ByteTransportMessageHandler<TSession, TConfig> : MessageHandler<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Id byte which precedes all messages all messages of this type.
		/// </summary>
		public sealed override byte Id => 2;

		internal readonly ResponseWait<ByteTransportWaitHandle<TSession, TConfig>> send_operation = new ResponseWait<ByteTransportWaitHandle<TSession, TConfig>>();

		internal readonly ResponseWait<ByteTransportWaitHandle<TSession, TConfig>> receive_operation = new ResponseWait<ByteTransportWaitHandle<TSession, TConfig>>();


		public ByteTransportMessageHandler(TSession session) : base(session) {
			Handlers.Add((byte)ByteTransportMessageAction.Request, Request);
			Handlers.Add((byte)ByteTransportMessageAction.Error, Error);
			Handlers.Add((byte)ByteTransportMessageAction.ReceiveReady, ReceiveReady);
			Handlers.Add((byte)ByteTransportMessageAction.Write, Write);
			Handlers.Add((byte)ByteTransportMessageAction.Close, Close);
		}



		private void Error(byte action_handler, MqMessage message) {
			throw new NotImplementedException();
		}

		/*public void ReadyReceive(ushort transport_id) {
			var ready_frame = Session.CreateFrame(new byte[2], MqFrameType.Last);
			ready_frame.Write(0, transport_id);

			SendHandlerMessage((byte)ByteTransportMessageAction.ReceiveReady, ready_frame.ToMessage());
		}*/

		public ByteTransport<TSession, TConfig> CreateTransport() {
			var handle = send_operation.CreateWaitHandle(null);
			handle.ByteTransport = new ByteTransport<TSession, TConfig>(this, handle.Id, ByteTransport<TSession, TConfig>.Mode.Send);

			var request_frame = Session.CreateFrame(new byte[2], MqFrameType.Last);
			request_frame.Write(0, handle.Id);

			SendHandlerMessage((byte)ByteTransportMessageAction.Request, request_frame.ToMessage());

			return handle.ByteTransport;
		}

		public ByteTransport<TSession, TConfig> GetRecieveTransport(ushort id) {
			return receive_operation[id].ByteTransport;
		}

		private void Request(byte action_id, MqMessage message) {
			ushort handle_id = message[0].ReadUInt16(0);

			var transport = new ByteTransport<TSession, TConfig>(this, handle_id, ByteTransport<TSession, TConfig>.Mode.Receive);

			var handle = receive_operation.CreateWaitHandle(handle_id);
			handle.ByteTransport = transport;

			SendHandlerMessage((byte)ByteTransportMessageAction.ReceiveReady, message);
		}

		private void ReceiveReady(byte action_handler, MqMessage message) {
			ushort handle_id = message[0].ReadUInt16(0);

			var transport = (IByteTransport) send_operation[handle_id].ByteTransport;

			if (transport.OnReady() == false) {
				Session.Close(SocketCloseReason.ApplicationError);
			}
		}

		private void Write(byte action_handler, MqMessage message) {
			ushort handle_id = message[0].ReadUInt16(0);

			var transport = (IByteTransport)receive_operation[handle_id].ByteTransport;

			transport.OnReceive(message);

			if (transport.OnReady() == false) {
				Session.Close(SocketCloseReason.ApplicationError);
			}

		}


		private void Close(byte action_handler, MqMessage message) {
			ushort handle_id = message[0].ReadUInt16(0);

			var transport = (IByteTransport)receive_operation[handle_id].ByteTransport;

			transport.SendClose();
			transport.OnReady();

		}



	}
}
