using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue {
	

	/// <summary>
	/// Mailbox containing inbox, outbox and the logic to process both.
	/// </summary>
	public class MqMailbox : IDisposable {

		private bool is_running = true;

		/// <summary>
		/// The postmaster for this client/server.
		/// </summary>
		private readonly MqPostmaster postmaster;


		private readonly MqSession session;

		/// <summary>
		/// Session reference.  If this mailbox is run as a client mailbox, this is then null.
		/// </summary>
		public MqSession Session => session;

		/// <summary>
		/// Internal framebuilder for this instance.
		/// </summary>
		private readonly MqFrameBuilder frame_builder;

		//private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Total bytes the inbox has remaining to process.
		/// </summary>
		private int inbox_byte_count;

		/// <summary>
		/// Reference to the current message being processed by the inbox.
		/// </summary>
		private MqMessage message;

		/// <summary>
		/// Outbox message queue.  Internally used to store Messages before being sent to the wire by the postmaster.
		/// </summary>
		private readonly ConcurrentQueue<MqMessage> outbox = new ConcurrentQueue<MqMessage>();

		/// <summary>
		/// Inbox byte queue.  Internally used to store the raw frame bytes before while waiting to be processed by the postmaster.
		/// </summary>
		private readonly ConcurrentQueue<byte[]> inbox_bytes = new ConcurrentQueue<byte[]>();

		/// <summary>
		/// Event fired when a new message has been processed by the postmaster and ready to be read.
		/// </summary>
		public event EventHandler<IncomingMessageEventArgs> IncomingMessage;

		/// <summary>
		/// Inbox to containing new messages received.
		/// </summary>
		public ConcurrentQueue<MqMessage> Inbox { get; } = new ConcurrentQueue<MqMessage>();

		/// <summary>
		/// Initializes a new instance of the MqMailbox class.
		/// </summary>
		/// <param name="postmaster">Reference to the postmaster for this instance.</param>
		/// <param name="session">Session from the server for this instance.</param>
		public MqMailbox(MqPostmaster postmaster, MqSession session) {
			this.postmaster = postmaster;
			this.session = session;
			frame_builder = new MqFrameBuilder();
		}

		/// <summary>
		/// Adds bytes from the client/server reading methods to be processed by the postmaster.
		/// </summary>
		/// <param name="buffer">Buffer of bytes to read. Does not copy the bytes to the buffer.</param>
		internal void EnqueueIncomingBuffer(byte[] buffer) {
			if (is_running == false) {
				return;
			}

			inbox_bytes.Enqueue(buffer);

			postmaster.SignalRead(this);

			Interlocked.Add(ref inbox_byte_count, buffer.Length);
		}


		/// <summary>
		/// Adds a message to the outbox to be processed by the postmaster.
		/// </summary>
		/// <param name="out_message">Message to send.</param>
		internal void EnqueueOutgoingMessage(MqMessage out_message) {
			if (is_running == false) {
				return;
			}

			outbox.Enqueue(out_message);

			// Signal the workers that work is to be done.
			postmaster.SignalWrite(this);
		}

		/// <summary>
		/// Stops the mailbox from sending and receiving.
		/// </summary>
		/// <param name="frame">Last frame to pass over the connection.</param>
		public void Stop(MqFrame frame) {
			if (is_running == false) {
				return;
			}

			is_running = false;

			MqMessage msg;
			if (outbox.IsEmpty == false) {
				while (outbox.TryDequeue(out msg)) { }
			}

			msg = new MqMessage(frame);
			outbox.Enqueue(msg);

			ProcessOutbox();
		}

		
		/// <summary>
		/// Sends a queue of bytes to the connected client/server.
		/// </summary>
		/// <param name="buffer_queue">Queue of bytes to send to the wire.</param>
		/// <param name="length">Total length of the bytes in the queue to send.</param>
		private void SendBufferQueue(Queue<byte[]> buffer_queue, int length) {
			var buffer = new byte[length];
			var offset = 0;

			while (buffer_queue.Count > 0) {
				var bytes = buffer_queue.Dequeue();
				Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);

				// Increment the offset.
				offset += bytes.Length;
			}

			
			// This will block 
			session.Send(buffer, 0, buffer.Length);
		}


		/// <summary>
		/// Internally called method by the postmaster on a different thread to send all messages in the outbox.
		/// </summary>
		internal void ProcessOutbox() {
			MqMessage result;
			var length = 0;
			var buffer_queue = new Queue<byte[]>();

			while (outbox.TryDequeue(out result)) {
				result.PrepareSend();
				foreach (var frame in result.Frames) {
					var frame_size = frame.FrameSize;
					// If this would overflow the max client buffer size, send the full buffer queue.
					if (length + frame_size > MqFrame.MaxFrameSize + MqFrame.HeaderLength) {
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
			//postmaster.SignalWriteComplete(this);
		}

		/// <summary>
		/// Internal method called by the postmaster on a different thread to process all bytes in the inbox.
		/// </summary>
		internal void ProcessIncomingQueue() {
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

					session.CloseConnection(SocketCloseReason.ProtocolError);
					break;
				}

				var frame_count = frame_builder.Frames.Count;
				//logger.Debug("Connector {0}: Parsed {1} frames.", Connection.Id, frame_count);

				for (var i = 0; i < frame_count; i++) {
					var frame = frame_builder.Frames.Dequeue();
					
					// Determine if this frame is a command type.  If it is, process it and don't add it to the message.
					if (frame.FrameType == MqFrameType.Command) {
						session.ProcessCommand(frame);
						continue;
					}

					message.Add(frame);

					if (frame.FrameType != MqFrameType.EmptyLast && frame.FrameType != MqFrameType.Last) {
						continue;
					}
					Inbox.Enqueue(message);
					message = new MqMessage();
					new_message = true;
				}
			}
			//postmaster.SignalReadComplete(this);

			if (new_message) {
				IncomingMessage?.Invoke(this, new IncomingMessageEventArgs(this, session));
			}
		}

		public override string ToString() {
			return $"Inbox: {Inbox.Count} ({inbox_byte_count} bytes); Outbox: {outbox.Count}";
		}

		/// <summary>
		/// Releases all resources held by this object.
		/// </summary>
		public void Dispose() {
			IncomingMessage = null;
		}
	}
}