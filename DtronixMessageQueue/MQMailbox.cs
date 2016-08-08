using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SuperSocket.SocketBase;

namespace DtronixMessageQueue {
	public class MqMailbox : IDisposable {
		private readonly MqPostmaster postmaster;
		private readonly MqClient client;
		private readonly MqSession session;
		private readonly MqFrameBuilder frame_builder;

		//private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private int inbox_byte_count;

		private MqMessage message;

		private readonly ConcurrentQueue<MqMessage> outbox = new ConcurrentQueue<MqMessage>();

		private readonly ConcurrentQueue<byte[]> inbox_bytes = new ConcurrentQueue<byte[]>();

		public event EventHandler<IncomingMessageEventArgs> IncomingMessage;

		public ConcurrentQueue<MqMessage> Inbox { get; } = new ConcurrentQueue<MqMessage>();

		public MqClient Client => client;

		public MqSession Session => session;

		public MqMailbox(MqPostmaster postmaster, MqSession session) {
			this.postmaster = postmaster;
			this.session = session;
			frame_builder = new MqFrameBuilder(postmaster);
		}

		public MqMailbox(MqPostmaster postmaster, MqClient client) {
			this.postmaster = postmaster;
			this.client = client;
			frame_builder = new MqFrameBuilder(postmaster);
		}

		internal void EnqueueIncomingBuffer(byte[] buffer) {
			inbox_bytes.Enqueue(buffer);

			postmaster.SignalRead(this);

			Interlocked.Add(ref inbox_byte_count, buffer.Length);
		}


		internal void EnqueueOutgoingMessage(MqMessage out_message) {
			outbox.Enqueue(out_message);

			// Signal the workers that work is to be done.
			postmaster.SignalWrite(this);
		}


		private void SendBufferQueue(Queue<byte[]> buffer_queue, int length) {
			var buffer = new byte[length + 3];

			// Setup the header for the packet.
			var length_bytes = BitConverter.GetBytes((ushort) length);
			buffer[0] = 0;
			buffer[1] = length_bytes[0];
			buffer[2] = length_bytes[1];

			var offset = 3;

			while (buffer_queue.Count > 0) {
				var bytes = buffer_queue.Dequeue();
				Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);

				// Increment the offset.
				offset += bytes.Length;
			}

			if (client != null) {
				client.Send(buffer);
			} else {
				session.Send(buffer, 0, buffer.Length);
			}
		}

		//private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);


		internal void ProcessOutbox() {
			MqMessage result;
			var length = 0;
			var buffer_queue = new Queue<byte[]>();

			while (outbox.TryDequeue(out result)) {
				foreach (var frame in result.Frames) {
					var frame_size = frame.FrameLength;
					// If this would overflow the max client buffer size, send the full buffer queue.
					if (length + frame_size > postmaster.MaxFrameSize + 3) {
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
		}

		internal async void ProcessIncomingQueue() {
			if (message == null) {
				message = new MqMessage();
			}

			bool new_message = false;
			byte[] buffer;
			while (inbox_bytes.TryDequeue(out buffer)) {
				// Update the total bytes this 
				Interlocked.Add(ref inbox_byte_count, -buffer.Length);

				try {
					frame_builder.Write(buffer, 0, buffer.Length);
				} catch (InvalidDataException) {
					//logger.Error(ex, "Connector {0}: Client send invalid data.", Connection.Id);

					if (client != null) {
						await client.Close();
					} else {
						session.Close(CloseReason.ApplicationError);
					}

					break;
				}

				var frame_count = frame_builder.Frames.Count;
				//logger.Debug("Connector {0}: Parsed {1} frames.", Connection.Id, frame_count);

				for (var i = 0; i < frame_count; i++) {
					var frame = frame_builder.Frames.Dequeue();
					message.Add(frame);

					if (frame.FrameType != MqFrameType.EmptyLast && frame.FrameType != MqFrameType.Last) {
						continue;
					}
					Inbox.Enqueue(message);
					message = new MqMessage();
					new_message = true;
				}
			}
			if (new_message) {
				IncomingMessage?.Invoke(this, new IncomingMessageEventArgs(this));
			}
		}


		public void Dispose() {
			IncomingMessage = null;
		}
	}
}