using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc.MessageHandlers {
	public class ByteTransport<TSession, TConfig> : IByteTransport
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		public enum Mode {
			Send,
			Receive
		}

		private readonly ByteTransportMessageHandler<TSession, TConfig> handler;
		private readonly ushort id;

		private readonly Mode mode;

		private readonly MqMessageReader message_reader;
		private readonly MqMessageWriter message_writer;

		public MqMessage ReadBuffer { get; set; }

		private readonly SemaphoreSlim semaphore;

		private readonly object read_lock = new object();

		public event EventHandler<ByteTransportReceiveEventArgs> Receive;

		public ByteTransport(ByteTransportMessageHandler<TSession, TConfig> handler, ushort id, Mode mode) {
			this.handler = handler;
			this.id = id;
			this.mode = mode;

			if (mode == Mode.Receive) {
				message_reader = new MqMessageReader();
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

			if (ReadBuffer == null) {
				await semaphore.WaitAsync(cancellation_token);
			}


			if (message_reader.Message == null) {
				message_reader.Message = ReadBuffer;
				message_reader.Skip(2);
			}

			var total_read = message_reader.Read(buffer, offset, count);

			if (message_reader.IsAtEnd) {
				message_reader.Message = null;
			}

			return total_read;

		}

		/// <summary>
		/// Writes a byte array to the transport.  Writing is not thread safe.
		/// </summary>
		/// <param name="buffer">Buffer to write to the message.</param>
		/// <param name="index">Offset in the buffer to write from</param>
		/// <param name="count">Number of bytes to write to the message from the buffer.</param>
		public async void WriteAsync(byte[] buffer, int index, int count, CancellationToken cancellation_token) {
			// Wait for the server response that we are ready to send the next packet.
			await semaphore.WaitAsync(cancellation_token);

			message_writer.Write(id);
			message_writer.Write(buffer, index, count);
			
			handler.SendHandlerMessage((byte)ByteTransportMessageAction.Write, message_writer.ToMessage(true));


		}
	}

	public interface IByteTransport {
		bool OnReady();
		void OnReceive(MqMessage message);
	}
}
