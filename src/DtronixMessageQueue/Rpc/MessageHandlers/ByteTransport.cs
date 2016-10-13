using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc.MessageHandlers {
	public class ByteTransport<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {
		public long Length { get; set; }

		private readonly TSession session;
		private readonly ushort id;

		private readonly MqMessageReader message_reader;
		private readonly MqMessageWriter message_writer;

		private ConcurrentQueue<MqMessage> read_buffer;

		private readonly SemaphoreSlim read_semaphore;

		private readonly object read_lock = new object();

		public event EventHandler<ByteTransportReceiveEventArgs> Receive;

		public ByteTransport(TSession session, ushort id, long length) {
			Length = length;
			this.session = session;
			this.id = id;
			message_reader = new MqMessageReader();
			read_semaphore = new SemaphoreSlim(0, 1);
		}

		public ByteTransport(TSession session) {
			this.session = session;
			message_writer = new MqMessageWriter(session.Config);
		}

		public void OnReceive(MqMessage message) {
			read_buffer.Enqueue(message);

			lock (read_lock) {
				if (read_semaphore.CurrentCount == 0) {
					read_semaphore.Release();
				}
			}
			
		}

		/// <summary>
		/// Reads data from the byte transport.
		/// </summary>
		/// <param name="buffer">Buffer to read into.</param>
		/// <param name="offset">Offset in the buffer to start copying to.</param>
		/// <param name="count">Number of bytes to try to read into the buffer.</param>
		public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation_token) {
			if (read_semaphore == null) {
				throw new InvalidOperationException("Byte transport is set to write mode.  Can not read when in write more.");
			}

			if (read_buffer.IsEmpty) {
				// ReSharper disable once InconsistentlySynchronizedField
				await read_semaphore.WaitAsync(cancellation_token);
			}


			if (message_reader.Message == null) {
				MqMessage message;
				if (read_buffer.TryDequeue(out message) == false) {
					return 0;
				}

				message_reader.Message = message;
				message_reader.Skip(3);
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
		public void Write(byte[] buffer, int index, int count) {
			message_writer.Write((byte) 2);
			message_writer.Write((byte) ByteTransportMessageType.Write);
			message_writer.Write(id);
			message_writer.Write(buffer, index, count);

			session.Send(message_writer.ToMessage(true));

		}
	}
}
