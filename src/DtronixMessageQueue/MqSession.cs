using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DtronixMessageQueue.TcpSocket;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Session to handle all reading/writing for a socket session.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public abstract class MqSession<TSession, TConfig> : TcpSocketSession<TSession, TConfig>
        where TSession : MqSession<TSession, TConfig>, new()
        where TConfig : MqConfig
    {
        /// <summary>
        /// Reference to the current message being processed by the inbox.
        /// </summary>
        private MqMessage _processMessage;

        /// <summary>
        /// Internal frame builder for this instance.
        /// </summary>
        private MqFrameBuilder _frameBuilder;

        /// <summary>
        /// Outbox message queue.  Internally used to store Messages before being sent to the wire by the Postmaster.
        /// </summary>
        private readonly ConcurrentQueue<MqMessage> _outbox = new ConcurrentQueue<MqMessage>();

        /// <summary>
        /// Inbox byte queue.  Internally used to store the raw frame bytes before while waiting to be processed by the Postmaster.
        /// </summary>
        private readonly ConcurrentQueue<byte[]> _inboxBytes = new ConcurrentQueue<byte[]>();

        private SemaphoreSlim _sendingSemaphore;

        private SemaphoreSlim _receivingSemaphore;

        /// <summary>
        /// Event fired when a new message has been processed by the Postmaster and ready to be read.
        /// </summary>
        public event EventHandler<IncomingMessageEventArgs<TSession, TConfig>> IncomingMessage;


        protected override void OnSetup()
        {
            _frameBuilder = new MqFrameBuilder(Config);
            _sendingSemaphore = new SemaphoreSlim(Config.MaxQueuedOutgoingMessages, Config.MaxQueuedOutgoingMessages);
            _receivingSemaphore = new SemaphoreSlim(Config.MaxQueuedInboundPackets, Config.MaxQueuedInboundPackets);

            InboxProcessor.Register(Id, ProcessIncomingQueue);
            OutboxProcessor.Register(Id, ProcessOutbox);
        }

        /// <summary>
        /// Adds bytes from the client/server reading methods to be processed by the Postmaster.
        /// </summary>
        /// <param name="buffer">Buffer of bytes to read. Does not copy the bytes to the buffer.</param>
        protected override void HandleIncomingBytes(byte[] buffer)
        {
            if (CurrentState != State.Connected)
            {
                return;
            }

            _receivingSemaphore.Wait();

            _inboxBytes.Enqueue(buffer);
            InboxProcessor.QueueOnce(Id);
        }

        /// <summary>
        /// Sends a queue of bytes to the connected client/server.
        /// </summary>
        /// <param name="bufferQueue">QueueOnce of bytes to send to the wire.</param>
        /// <param name="length">Total length of the bytes in the queue to send.</param>
        private void SendBufferQueue(Queue<byte[]> bufferQueue, int length, bool last)
        {
            var buffer = new byte[length];
            var offset = 0;

            while (bufferQueue.Count > 0)
            {
                var bytes = bufferQueue.Dequeue();
                Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);

                // Increment the offset.
                offset += bytes.Length;
            }


            // This will block 
            Send(new ReadOnlyMemory<byte>(buffer, 0, buffer.Length));
        }


        /// <summary>
        /// Internally called method by the Postmaster on a different thread to send all messages in the outbox.
        /// </summary>
        /// <returns>True if messages were sent.  False if nothing was sent.</returns>
        private void ProcessOutbox()
        {
            MqMessage message;
            var length = 0;
            var bufferQueue = new Queue<byte[]>();

            while (_outbox.TryDequeue(out message))
            {
                if(CurrentState != State.Closed)
                    _sendingSemaphore.Release();

                message.PrepareSend();
                for (var i = 0; i < message.Count; i++)
                {
                    var frameSize = message[i].FrameSize;

                    // If this would overflow the max client buffer size, send the full buffer queue.
                    if (length + frameSize > Config.FrameBufferSize + MqFrame.HeaderLength)
                    {
                        SendBufferQueue(bufferQueue, length, false);

                        // Reset the length to 0;
                        length = 0;
                    }
                    bufferQueue.Enqueue(message[i].RawFrame());

                    // Increment the total buffer length.
                    length += frameSize;
                }
            }

            if (bufferQueue.Count == 0)
            {
                return;
            }

            // Send the last of the buffer queue.
            SendBufferQueue(bufferQueue, length, true);
        }

        /// <summary>
        /// Internal method called by the Postmaster on a different thread to process all bytes in the inbox.
        /// </summary>
        /// <returns>True if incoming queue was processed; False if nothing was available for process.</returns>
        private void ProcessIncomingQueue()
        {
            if (_processMessage == null)
            {
                _processMessage = new MqMessage();
            }

            Queue<MqMessage> messages = null;
            byte[] buffer;
            while (_inboxBytes.TryDequeue(out buffer))
            {

                if (CurrentState != State.Connected)
                    return;

                _receivingSemaphore.Release();

                if (buffer == null)
                {
                    Close(CloseReason.Closing);
                    return;
                }

                try
                {
                    _frameBuilder.Write(buffer);
                }
                catch (InvalidDataException)
                {
                    //logger.Error(ex, "Connector {0}: Client send invalid data.", Connection.Id);

                    Close(CloseReason.ProtocolError);
                    break;
                }

                var frameCount = _frameBuilder.Frames.Count;
                //logger.Debug("Connector {0}: Parsed {1} frames.", Connection.Id, frame_count);

                for (var i = 0; i < frameCount; i++)
                {
                    if (CurrentState != State.Connected)
                        return;

                    var frame = _frameBuilder.Frames.Dequeue();

                    // Do nothing if this is a ping frame.
                    if (frame.FrameType == MqFrameType.Ping)
                    {
                        if (SocketHandler.Mode == TcpSocketMode.Server)
                        {
                            // Re-send ping frame back to the client to refresh client connection timeout timer.
                            Send(CreateFrame(null, MqFrameType.Ping));
                        }
                        continue;
                    }

                    // Determine if this frame is a command type.  If it is, process it and don't add it to the message.
                    if (frame.FrameType == MqFrameType.Command)
                    {
                        ProcessCommand(frame);
                        continue;
                    }

                    _processMessage.Add(frame);

                    if (frame.FrameType != MqFrameType.EmptyLast && frame.FrameType != MqFrameType.Last)
                    {
                        continue;
                    }

                    if (messages == null)
                    {
                        messages = new Queue<MqMessage>();
                    }

                    messages.Enqueue(_processMessage);
                    _processMessage = new MqMessage();
                }
            }

            if (messages == null)
            {
                return;
            }

            OnIncomingMessage(this, new IncomingMessageEventArgs<TSession, TConfig>(messages, (TSession)this));
            
        }


        /// <summary>
        /// Event fired when one or more new messages are ready for use.
        /// </summary>
        /// <param name="sender">Originator of call for this event.</param>
        /// <param name="e">Event args for the message.</param>
        protected virtual void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession, TConfig> e)
        {
            IncomingMessage?.Invoke(sender, e);
        }


        /// <summary>
        /// Closes this session with the specified reason.
        /// Notifies the recipient connection the reason for the session's closure.
        /// </summary>
        /// <param name="reason">Reason for closing this session.</param>
        public override void Close(CloseReason reason)
        {
            if (CurrentState == State.Closed && reason != CloseReason.ConnectionRefused)
                return;

            /*
            MqFrame closeFrame = null;
            if (CurrentState == State.Connected || reason == CloseReason.ConnectionRefused)
            {
                closeFrame = CreateFrame(new byte[2], MqFrameType.Command);

                closeFrame.Write(0, (byte)0); // Close
                closeFrame.Write(1, (byte)reason);
            }

            // If we are passed a closing frame, then send it to the other connection.
            if (closeFrame != null)
            {
                MqMessage msg;
                byte[] buffer;
                if (_outbox.IsEmpty == false)
                {
                    while (_outbox.TryDequeue(out msg))
                        _sendingSemaphore.Release();


                    while (_inboxBytes.TryDequeue(out buffer))
                        _receivingSemaphore.Release();
                }

                msg = new MqMessage(closeFrame);
                _outbox.Enqueue(msg);

                // Take one wait to send the close packet.
                _sendingSemaphore.Wait();

                // QueueOnce the last bit of data.
                ProcessOutbox();
            }*/

            base.Close(reason);

            _receivingSemaphore.Dispose();
            _sendingSemaphore.Dispose();
            _frameBuilder.Dispose();
        }

        /// <summary>
        /// Adds a frame to the outbox to be processed.
        /// </summary>
        /// <param name="frame">Frame to send.</param>
        public void Send(MqFrame frame)
        {
            Send(new MqMessage(frame));
        }

        /// <summary>
        /// Sends a message to the session's client.
        /// </summary>
        /// <param name="message">Message to send.</param>
        public void Send(MqMessage message)
        {
            if (message.Count == 0)
            {
                return;
            }
            if (CurrentState != State.Connected)
            {
                return;
            }

            _sendingSemaphore.Wait();
            _outbox.Enqueue(message);

            OutboxProcessor.QueueOnce(Id);
        }

        /// <summary>
        /// Creates a frame with the specified bytes and the current configurations.
        /// </summary>
        /// <param name="bytes">Bytes to put in the frame.</param>
        /// <returns>Configured frame.</returns>
        public MqFrame CreateFrame(byte[] bytes)
        {
            return Utilities.CreateFrame(bytes, MqFrameType.Unset, Config);
        }

        /// <summary>
        /// Creates a frame with the specified bytes and the current configurations.
        /// </summary>
        /// <param name="bytes">Bytes to put in the frame.</param>
        /// <param name="type">Type of frame to create.</param>
        /// <returns>Configured frame.</returns>
        public MqFrame CreateFrame(byte[] bytes, MqFrameType type)
        {
            return Utilities.CreateFrame(bytes, type, Config);
        }


        /// <summary>
        /// Processes an incoming command frame from the connection.
        /// </summary>
        /// <param name="frame">Command frame to process.</param>
        protected virtual void ProcessCommand(MqFrame frame)
        {
            var commandType = (MqCommandType)frame.ReadByte(0);

            switch (commandType)
            {
                case MqCommandType.Disconnect:
                    Close((CloseReason)frame.ReadByte(1));
                    break;

                default:
                    Close(CloseReason.ProtocolError);
                    break;
            }
        }

        /// <summary>
        /// String representation of the active session.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            return $"{SocketHandler.Mode} RcpSocketSession; Reading {_inboxBytes.Count} byte packets; Sending {_outbox.Count} messages.";
        }


    }
}