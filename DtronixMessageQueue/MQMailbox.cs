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

		private readonly MQConnector.Connection connection;
		private int inbox_byte_count;

		private MQMessage message;

		private readonly ConcurrentQueue<MQMessage> inbox = new ConcurrentQueue<MQMessage>();
		private readonly ConcurrentQueue<MQMessage> outbox = new ConcurrentQueue<MQMessage>();

		private readonly ConcurrentQueue<byte[]> inbox_bytes = new ConcurrentQueue<byte[]>();

		public event EventHandler<IncomingMessageEventArgs> OnIncomingMessage;

		public MQMailbox(MQConnector.Connection connection) {
			this.connection = connection;
		}

		public void QueueOutgoingMessage(MQMessage message) {
			outbox.Enqueue(message);
		}

		public void EnqueueIncomingBuffer(byte[] buffer) {
			inbox_bytes.Enqueue(buffer);

			// Update the total bytes this 
			Interlocked.Add(ref inbox_byte_count, buffer.Length);


		}

		private byte[] DequeueIncomingBuffer() {
			byte[] buffer;
			if (inbox_bytes.TryDequeue(out buffer)) {

				// Subtract this length from the total length list.
				Interlocked.Add(ref inbox_byte_count, -buffer.Length);
			}

			return buffer;
		}

		public void ProcessIncomingQueue() {
			if (message == null) {
				message = new MQMessage();
			}

			//if (e.BytesTransferred != 4000) {
			//	return;
			//}

			try {
				connection.FrameBuilder.Write(e.Buffer, e.Offset, e.BytesTransferred);
			} catch (InvalidDataException ex) {
				logger.Error(ex, "Connector {0}: Client send invalid data.", connection.Id);

				var connection_server = connection.Connector as MQServer;

				if (connection_server != null) {
					connection_server.CloseConnection(connection);
				}
				return;
			}

			var frame_count = connection.FrameBuilder.Frames.Count;
			logger.Debug("Connector {0}: Parsed {1} frames.", connection.Id, frame_count);

			for (var i = 0; i < frame_count; i++) {
				var frame = connection.FrameBuilder.Frames.Dequeue();
				message.Add(frame);

				if (frame.FrameType == MQFrameType.EmptyLast || frame.FrameType == MQFrameType.Last) {

					inbox.Enqueue(message);
					message = new MQMessage();

					OnIncomingMessage?.Invoke(this, new IncomingMessageEventArgs(connection.Mailbox, connection.Id));
				}
			}
		}

		

		public void Dispose() {
			throw new NotImplementedException();
		}
	}
}
