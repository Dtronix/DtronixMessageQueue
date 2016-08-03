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

		private bool is_inbox_processing;
		private bool is_outbox_processing;

		public ConcurrentQueue<MQMessage> Inbox { get; } = new ConcurrentQueue<MQMessage>();
		private readonly ConcurrentQueue<MQMessage> outbox = new ConcurrentQueue<MQMessage>();

		private readonly ConcurrentQueue<byte[]> inbox_bytes = new ConcurrentQueue<byte[]>();

		public event EventHandler<IncomingMessageEventArgs> IncomingMessage;

		public MQMailbox(MQConnection connection) {
			Connection = connection;
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


		public void EnqueueOutgoingMessage(MQMessage out_message) {
			outbox.Enqueue(out_message);

			// Signal the workers that work is to be done.
			if (is_outbox_processing == false) {
				Connection.Connector.Postmaster.WriteOperations.TryAdd(this);
			}

		}


		private void SendBufferQueue(Queue<byte[]> buffer_queue, int length) {
			var buffer = new byte[length];
			var offset = 0;

			while (buffer_queue.Count > 0) {
				var bytes = buffer_queue.Dequeue();
				Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);

				// Increment the offset.
				offset += bytes.Length;
			}

			Connection.Connector.Send(Connection, buffer, 0, length);
		}

		internal void ProcessOutbox() {
			//if (is_outbox_processing) {
				//return;
			//}

			is_outbox_processing = true;
			MQMessage result;
			var length = 0;
			var buffer_queue = new Queue<byte[]>();

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

			if (buffer_queue.Count > 0) {
				// Send the last of the buffer queue.
				SendBufferQueue(buffer_queue, length);
			}
			is_outbox_processing = false;
		}

		internal void ProcessIncomingQueue() {
			if (is_inbox_processing) {
				return;
			}

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

					if (frame.FrameType != MQFrameType.EmptyLast && frame.FrameType != MQFrameType.Last) {
						continue;
					}
					Inbox.Enqueue(message);
					message = new MQMessage();

					IncomingMessage?.Invoke(this, new IncomingMessageEventArgs(Connection));
				}
			}

			is_inbox_processing = false;
		}

		

		public void Dispose() {
			IncomingMessage = null;
		}
	}
}
