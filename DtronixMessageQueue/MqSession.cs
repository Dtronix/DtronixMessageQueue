using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue {


	public class MqSession : SocketSession {


		private bool is_running = true;

		/// <summary>
		/// The Postmaster for this client/server.
		/// </summary>
		public MqPostmaster Postmaster { get; set; }

		/// <summary>
		/// Internal framebuilder for this instance.
		/// </summary>
		private readonly MqFrameBuilder frame_builder = new MqFrameBuilder();

		/// <summary>
		/// Total bytes the inbox has remaining to process.
		/// </summary>
		private int inbox_byte_count;

		/// <summary>
		/// Reference to the current message being processed by the inbox.
		/// </summary>
		private MqMessage message;

		/// <summary>
		/// Outbox message queue.  Internally used to store Messages before being sent to the wire by the Postmaster.
		/// </summary>
		private readonly ConcurrentQueue<MqMessage> outbox = new ConcurrentQueue<MqMessage>();

		/// <summary>
		/// Inbox byte queue.  Internally used to store the raw frame bytes before while waiting to be processed by the Postmaster.
		/// </summary>
		private readonly ConcurrentQueue<byte[]> inbox_bytes = new ConcurrentQueue<byte[]>();

		/// <summary>
		/// Event fired when a new message has been processed by the Postmaster and ready to be read.
		/// </summary>
		public event EventHandler<IncomingMessageEventArgs> IncomingMessage;


		/// <summary>
		/// User supplied token used to pass a related object around with this session.
		/// </summary>
		public object Token { get; set; }

		/// <summary>
		/// Adds bytes from the client/server reading methods to be processed by the Postmaster.
		/// </summary>
		/// <param name="buffer">Buffer of bytes to read. Does not copy the bytes to the buffer.</param>
		protected override void HandleIncomingBytes(byte[] buffer) {
			if (is_running == false) {
				return;
			}

			inbox_bytes.Enqueue(buffer);
			Interlocked.Add(ref inbox_byte_count, buffer.Length);

			Postmaster.SignalRead(this);
		}

		/// <summary>
		/// Adds a message to the outbox to be processed by the Postmaster.
		/// </summary>
		/// <param name="out_message">Message to send.</param>
		public void EnqueueOutgoingMessage(MqMessage out_message) {
			if (is_running == false) {
				return;
			}

			outbox.Enqueue(out_message);

			// Signal the workers that work is to be done.
			Postmaster.SignalWrite(this);
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
			Send(buffer, 0, buffer.Length);
		}


		/// <summary>
		/// Internally called method by the Postmaster on a different thread to send all messages in the outbox.
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
			//Postmaster.SignalWriteComplete(this);
		}

		/// <summary>
		/// Internal method called by the Postmaster on a different thread to process all bytes in the inbox.
		/// </summary>
		internal void ProcessIncomingQueue() {
			if (message == null) {
				message = new MqMessage();
			}

			Queue<MqMessage> messages = null;
			byte[] buffer;
			while (inbox_bytes.TryDequeue(out buffer)) {
				// Update the total bytes this 
				Interlocked.Add(ref inbox_byte_count, -buffer.Length);

				try {
					frame_builder.Write(buffer, 0, buffer.Length);
				} catch (InvalidDataException) {
					//logger.Error(ex, "Connector {0}: Client send invalid data.", Connection.Id);

					CloseConnection(SocketCloseReason.ProtocolError);
					break;
				}

				var frame_count = frame_builder.Frames.Count;
				//logger.Debug("Connector {0}: Parsed {1} frames.", Connection.Id, frame_count);

				for (var i = 0; i < frame_count; i++) {
					var frame = frame_builder.Frames.Dequeue();

					// Determine if this frame is a command type.  If it is, process it and don't add it to the message.
					if (frame.FrameType == MqFrameType.Command) {
						ProcessCommand(frame);
						continue;
					}

					message.Add(frame);

					if (frame.FrameType != MqFrameType.EmptyLast && frame.FrameType != MqFrameType.Last) {
						continue;
					}

					if (messages == null) {
						messages = new Queue<MqMessage>();
					}

					messages.Enqueue(message);
					message = new MqMessage();
				}
			}
			//Postmaster.SignalReadComplete(this);

			if (messages != null) {
				IncomingMessage?.Invoke(this, new IncomingMessageEventArgs(messages, this));
			}
		}


		public override void CloseConnection(SocketCloseReason reason) {
			if (CurrentState == State.Closed) {
				return;
			}

			MqFrame close_frame = null;
			if (CurrentState == State.Connected) {
				CurrentState = State.Closing;
				close_frame = new MqFrame(new byte[2]) {FrameType = MqFrameType.Command};
				close_frame.Write(0, (byte) 0);
				close_frame.Write(1, (byte) reason);
			}

			// If we are passed a closing frame, then send it to the other connection.
			if (close_frame != null) {
				MqMessage msg;
				if (outbox.IsEmpty == false) {
					while (outbox.TryDequeue(out msg)) {
					}
				}

				msg = new MqMessage(close_frame);
				outbox.Enqueue(msg);

				ProcessOutbox();
			}

			base.CloseConnection(reason);
		}


		/// <summary>
		/// Sends a message to the session's client.
		/// </summary>
		/// <param name="message">Message to send.</param>
		public void Send(MqMessage message) {
			/*if (Connected == false) {
				throw new InvalidOperationException("Can not send messages while disconnected from server.");
			}*/

			if (message.Count == 0) {
				return;
			}

			EnqueueOutgoingMessage(message);
		}


		/// <summary>
		/// Processes an incoming command frame from the connection.
		/// </summary>
		/// <param name="frame">Command frame to process.</param>
		internal void ProcessCommand(MqFrame frame) {
			var command_type = frame.ReadByte(0);

			switch (command_type) {
				case 0: // Closed
					CurrentState = State.Closing;
					CloseConnection((SocketCloseReason)frame.ReadByte(1));
					break;

			}
		}

		public override string ToString() {
			return $"MqSession; Reading {inbox_byte_count} bytes; Sending {outbox.Count} messages.";
		}
	}
}