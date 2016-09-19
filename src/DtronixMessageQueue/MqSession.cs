using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Amib.Threading;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue {

	/// <summary>
	/// Session to handle all reading/writing for a socket session.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public abstract class MqSession<TSession, TConfig> : SocketSession<TConfig>
		where TSession : MqSession<TSession, TConfig>, new()
		where TConfig : MqConfig {

		/// <summary>
		/// True if the socket is currently running.
		/// </summary>
		private bool is_running = true;

		/// <summary>
		/// Total bytes the inbox has remaining to process.
		/// </summary>
		private int inbox_byte_count;

		/// <summary>
		/// Reference to the current message being processed by the inbox.
		/// </summary>
		private MqMessage process_message;

		/// <summary>
		/// Internal framebuilder for this instance.
		/// </summary>
		private MqFrameBuilder frame_builder;

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
		public event EventHandler<IncomingMessageEventArgs<TSession, TConfig>> IncomingMessage;

		/// <summary>
		/// Base socket for this session.
		/// </summary>
		public SocketBase<TSession, TConfig> BaseSocket { get; set; }

		protected override void OnSetup() {
			frame_builder = new MqFrameBuilder(Config);
		}

		/// <summary>
		/// Adds bytes from the client/server reading methods to be processed by the Postmaster.
		/// </summary>
		/// <param name="buffer">Buffer of bytes to read. Does not copy the bytes to the buffer.</param>
		protected override void HandleIncomingBytes(byte[] buffer) {
			if (is_running == false) {
				return;
			}

			var inbox_was_empty = outbox.IsEmpty;

			inbox_bytes.Enqueue(buffer);

			if (inbox_was_empty || reader_pool.IsIdle) {
				reader_pool.QueueWorkItem(ProcessIncomingQueue, WorkItemPriority.Normal);
			}
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
		/// <returns>True if messages were sent.  False if nothing was sent.</returns>
		private void ProcessOutbox() {
			MqMessage message;
			var length = 0;
			var buffer_queue = new Queue<byte[]>();

			while (outbox.TryDequeue(out message)) {
				//Console.WriteLine("Wrote " + message);
				message.PrepareSend();
				foreach (var frame in message) {
					var frame_size = frame.FrameSize;

					// If this would overflow the max client buffer size, send the full buffer queue.
					if (length + frame_size > Config.FrameBufferSize + MqFrame.HeaderLength) {
						SendBufferQueue(buffer_queue, length);

						// Reset the length to 0;
						length = 0;
					}
					buffer_queue.Enqueue(frame.RawFrame());

					// Increment the total buffer length.
					length += frame_size;
				}
			}

			if (buffer_queue.Count == 0) {
				return;
			}

			// Send the last of the buffer queue.
			SendBufferQueue(buffer_queue, length);
		}

		/// <summary>
		/// Internal method called by the Postmaster on a different thread to process all bytes in the inbox.
		/// </summary>
		/// <returns>True if incoming queue was processed; False if nothing was available for process.</returns>
		private void ProcessIncomingQueue() {
			if (process_message == null) {
				process_message = new MqMessage();
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

					Close(SocketCloseReason.ProtocolError);
					break;
				}

				var frame_count = frame_builder.Frames.Count;
				//logger.Debug("Connector {0}: Parsed {1} frames.", Connection.Id, frame_count);

				for (var i = 0; i < frame_count; i++) {
					var frame = frame_builder.Frames.Dequeue();

					// Do nothing if this is a ping frame.
					if (frame.FrameType == MqFrameType.Ping) {
						continue;
					}

					// Determine if this frame is a command type.  If it is, process it and don't add it to the message.
					if (frame.FrameType == MqFrameType.Command) {
						ProcessCommand(frame);
						continue;
					}

					process_message.Add(frame);

					if (frame.FrameType != MqFrameType.EmptyLast && frame.FrameType != MqFrameType.Last) {
						continue;
					}

					if (messages == null) {
						messages = new Queue<MqMessage>();
					}

					messages.Enqueue(process_message);
					process_message = new MqMessage();
				}
			}

			if (messages == null) {
				return;
			}



			OnIncomingMessage(this, new IncomingMessageEventArgs<TSession, TConfig>(messages, (TSession) this));
		}


		/// <summary>
		/// Event fired when one or more new messages are ready for use.
		/// </summary>
		/// <param name="sender">Originator of call for this event.</param>
		/// <param name="e">Event args for the message.</param>
		protected virtual void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession, TConfig> e) {
			IncomingMessage?.Invoke(sender, e);
		}


		/// <summary>
		/// Closes this session with the specified reason.
		/// Notifies the other end of this connection the reason for the session's closure.
		/// </summary>
		/// <param name="reason">Reason for closing this session.</param>
		public override void Close(SocketCloseReason reason) {
			if (CurrentState == State.Closed) {
				return;
			}

			MqFrame close_frame = null;
			if (CurrentState == State.Connected) {
				CurrentState = State.Closing;
				
				close_frame = CreateFrame(new byte[2]);
				close_frame.FrameType = MqFrameType.Command;

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

				// Process the last bit of data.
				ProcessOutbox();
			}

			base.Close(reason);
		}

		/// <summary>
		/// Adds a frame to the outbox to be processed.
		/// </summary>
		/// <param name="frame">Frame to send.</param>
		public void Send(MqFrame frame) {
			Send(new MqMessage(frame));
		}

		/// <summary>
		/// Sends a message to the session's client.
		/// </summary>
		/// <param name="message">Message to send.</param>
		public void Send(MqMessage message) {
			if (message.Count == 0) {
				return;
			}
			if (is_running == false) {
				return;
			}

			var outbox_was_empty = outbox.IsEmpty;

			outbox.Enqueue(message);

			if (outbox_was_empty || writer_pool.IsIdle) {
				writer_pool.QueueWorkItem(ProcessOutbox, WorkItemPriority.Normal);
			}
		}

		/// <summary>
		/// Creates a frame with the specified bytes and the current configurations.
		/// </summary>
		/// <param name="bytes">Bytes to put in the frame.</param>
		/// <returns>Configured frame.</returns>
		public MqFrame CreateFrame(byte[] bytes) {
			return Utilities.CreateFrame(bytes, MqFrameType.Unset, Config);
		}

		/// <summary>
		/// Creates a frame with the specified bytes and the current configurations.
		/// </summary>
		/// <param name="bytes">Bytes to put in the frame.</param>
		/// <param name="type">Type of frame to create.</param>
		/// <returns>Configured frame.</returns>
		public MqFrame CreateFrame(byte[] bytes, MqFrameType type) {
			return Utilities.CreateFrame(bytes, type, Config);
		}


		/// <summary>
		/// Processes an incoming command frame from the connection.
		/// </summary>
		/// <param name="frame">Command frame to process.</param>
		protected virtual void ProcessCommand(MqFrame frame) {
			var command_type = frame.ReadByte(0);

			switch (command_type) {
				case 0: // Closed
					CurrentState = State.Closing;
					Close((SocketCloseReason)frame.ReadByte(1));
					break;

			}
		}

		/// <summary>
		/// String representation of the active session.
		/// </summary>
		/// <returns>String representation.</returns>
		public override string ToString() {
			return $"MqSession; Reading {inbox_byte_count} bytes; Sending {outbox.Count} messages.";
		}
	}
}