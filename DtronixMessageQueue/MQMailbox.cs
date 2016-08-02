using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace DtronixMessageQueue {
	public class MQMailbox : IDisposable {
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public readonly MQConnection Connection;
		private int inbox_byte_count;

		private MQMessage message;

		private bool is_inbox_processing = false;
		private bool is_outbox_processing = false;

		private readonly ConcurrentQueue<MQMessage> inbox = new ConcurrentQueue<MQMessage>();
		private readonly ConcurrentQueue<MQMessage> outbox = new ConcurrentQueue<MQMessage>();

		private readonly ConcurrentQueue<byte[]> inbox_bytes = new ConcurrentQueue<byte[]>();

		public event EventHandler<IncomingMessageEventArgs> OnIncomingMessage;

		public MQMailbox(MQConnection connection) {
			this.Connection = connection;
		}

		public void QueueOutgoingMessage(MQMessage message) {
			outbox.Enqueue(message);
		}

		public void EnqueueIncomingBuffer(byte[] buffer) {
			inbox_bytes.Enqueue(buffer);

			// Update the total bytes this 
			Interlocked.Add(ref inbox_byte_count, buffer.Length);

			// Signal the workers that work is to be done.
			if (is_inbox_processing == false) {
				Connection.Connector.Postmaster.ReadOperations.TryAdd(this);
			}
		}


		public void EnqueueOutgoingMessage(MQMessage message) {
			outbox.Enqueue(message);

			// Signal the workers that work is to be done.
			if (is_outbox_processing == false) {
				Connection.Connector.Postmaster.WriteOperations.TryAdd(this);
			}

		}


		private void SendBufferQueue(Queue<byte[]> buffer_queue, int length) {
			byte[] buffer = new byte[length];
			int offset = 0;

			while (buffer_queue.Count > 0) {
				var bytes = buffer_queue.Dequeue();
				Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);

				// Increment the offset.
				offset += offset;
			}

			Connection.Connector.Send(Connection, buffer, 0, length);
		}

		internal void ProcessOutbox() {
			MQMessage result;
			var length = 0;
			Queue<byte[]> buffer_queue = new Queue<byte[]>();

			while (outbox.TryDequeue(out result)) {
				foreach (var frame in result.Frames) {
					var frame_size = frame.FrameLength;
					// If this would overflow the max client buffer size, send the full buffer queue.
					if (length + frame_size > Connection.Connector.ClientBufferSize) {
						SendBufferQueue(buffer_queue, length);

						// Reset the length to 0;
						length = 0;
					}
					buffer_queue.Enqueue(frame.RawFrame());

					// Increment the total buffer length.
					length += frame_size;
				}
			}

			SendBufferQueue(buffer_queue, length);
		}

		internal void ProcessIncomingQueue() {
			is_inbox_processing = true;
			if (message == null) {
				message = new MQMessage();
			}

			byte[] buffer;
			while (inbox_bytes.TryDequeue(out buffer)) {
				// Update the total bytes this 
				Interlocked.Add(ref inbox_byte_count, -buffer.Length);

				try {
					Connection.FrameBuilder.Write(buffer, 0, buffer.Length);
				} catch (InvalidDataException ex) {
					logger.Error(ex, "Connector {0}: Client send invalid data.", Connection.Id);

					var connection_server = Connection.Connector as MQServer;

					connection_server?.CloseConnection(Connection);
					break;
				}

				var frame_count = Connection.FrameBuilder.Frames.Count;
				logger.Debug("Connector {0}: Parsed {1} frames.", Connection.Id, frame_count);

				for (var i = 0; i < frame_count; i++) {
					var frame = Connection.FrameBuilder.Frames.Dequeue();
					message.Add(frame);

					if (frame.FrameType == MQFrameType.EmptyLast || frame.FrameType == MQFrameType.Last) {

						inbox.Enqueue(message);
						message = new MQMessage();

						OnIncomingMessage?.Invoke(this, new IncomingMessageEventArgs(this, Connection.Id));
					}
				}
			}

			is_inbox_processing = false;
		}

		

		public void Dispose() {
			throw new NotImplementedException();
		}
	}
}
