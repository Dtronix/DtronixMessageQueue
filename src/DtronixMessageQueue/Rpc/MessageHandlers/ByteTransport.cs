using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc.MessageHandlers {
	public class ByteTransport<TSession, TConfig> : IByteTransport, IDisposable
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		public enum Mode {
			Send,
			Receive
		}

		private readonly ByteTransportMessageHandler<TSession, TConfig> handler;
		public readonly ushort Id;

		private readonly Mode mode;

		private readonly MqMessageReader message_reader;
		private readonly MqMessageWriter message_writer;

		public MqMessage ReadBuffer { get; set; }

		private bool is_closed;

		private readonly SemaphoreSlim semaphore;

		private readonly object read_lock = new object();

		private bool requested_buffer = false;

		private MqMessage ready_message;

		public ByteTransport(ByteTransportMessageHandler<TSession, TConfig> handler, ushort id, Mode mode) {
			this.handler = handler;
			this.Id = id;
			this.mode = mode;

			if (mode == Mode.Receive) {
				message_reader = new MqMessageReader();
				var ready_frame = handler.Session.CreateFrame(new byte[2], MqFrameType.Last);
				ready_frame.Write(0, id);

				ready_message = ready_frame.ToMessage();
			} else {
				message_writer = new MqMessageWriter(handler.Session.Config);
			}

			semaphore = new SemaphoreSlim(0, 1);
		}


		bool IByteTransport.OnReady() {
			if (semaphore.CurrentCount == 0) {
				semaphore.Release();
				return true;
			}

			return false;
		}



		void IByteTransport.OnReceive(MqMessage message) {
			ReadBuffer = message;
			requested_buffer = false;
		}

		void IByteTransport.SendClose() {
			is_closed = true;
		}

		/// <summary>
		/// Reads data from the byte transport.
		/// </summary>
		/// <param name="buffer">Buffer to read into.</param>
		/// <param name="offset">Offset in the buffer to start copying to.</param>
		/// <param name="count">Number of bytes to try to read into the buffer.</param>
		public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation_token) {

			if (semaphore == null) {
				throw new InvalidOperationException("Byte transport is set to write mode.  Can not read when in write more.");
			}

			if (ReadBuffer == null && is_closed == false) {
				await semaphore.WaitAsync(cancellation_token);
			}

			if (requested_buffer == false) {
				handler.SendHandlerMessage((byte) ByteTransportMessageAction.ReceiveReady, ready_message);
				requested_buffer = true;
			}			

			if (message_reader.Message == null && ReadBuffer != null) {
				message_reader.Message = ReadBuffer;
				message_reader.Skip(2);

				ReadBuffer = null;
			}

			var total_read = message_reader.Read(buffer, offset, count);

			if (message_reader.IsAtEnd) {
				message_reader.Message = null;

				if (is_closed == false) {
					
				}

			}

			return total_read;

		}

		/// <summary>
		/// Writes a byte array to the transport.  Writing is not thread safe.
		/// </summary>
		/// <param name="buffer">Buffer to write to the message.</param>
		/// <param name="index">Offset in the buffer to write from</param>
		/// <param name="count">Number of bytes to write to the message from the buffer.</param>
		public async Task WriteAsync(byte[] buffer, int index, int count, CancellationToken cancellation_token) {
			// Wait for the server response that we are ready to send the next packet.
			try {
				await semaphore.WaitAsync(cancellation_token);
			} catch (OperationCanceledException ex) {
				Close();
				return;
			}

			

			message_writer.Write(Id);
			message_writer.Write(buffer, index, count);
			
			handler.SendHandlerMessage((byte)ByteTransportMessageAction.Write, message_writer.ToMessage(true));


		}

		public void Close() {
			if (is_closed) {
				return;
			}

			is_closed = true;

			if (mode == Mode.Send) {
				message_writer.Write(Id);
				handler.SendHandlerMessage((byte) ByteTransportMessageAction.Close, message_writer.ToMessage(true));
			}
		}

		public void Dispose() {
			Close();
		}
	}

	public interface IByteTransport {
		bool OnReady();
		void OnReceive(MqMessage message);
		void SendClose();
	}
}
