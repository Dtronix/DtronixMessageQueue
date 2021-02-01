using System;
using System.Buffers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Logging;

namespace DtronixMessageQueue.Socket
{
    /// <summary>
    /// Base socket session to be sub-classes by the implementer.
    /// </summary>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    /// <typeparam name="TSession">Session for this connection.</typeparam>
    public abstract class SocketSession<TSession, TConfig> : IDisposable, ISetupSocketSession
        where TSession : SocketSession<TSession, TConfig>, new()
        where TConfig : SocketConfig
    {
        public const byte ProtocolVersion = 1;

        private TConfig _config;

        /// <summary>
        /// Configurations for the associated socket.
        /// </summary>
        public TConfig Config => _config;

        /// <summary>
        /// Id for this session
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// State that this socket is in.  Can only perform most operations when the socket is in a Connected state.
        /// </summary>
        public SocketSessionState CurrentSocketSessionState { get; protected set; }

        /// <summary>
        /// The last time that this session received a message.
        /// </summary>
        private DateTime _lastReceived = DateTime.UtcNow;

        /// <summary>
        /// Last time the session received anything from the socket.  Time in UTC.
        /// </summary>
        public DateTime LastReceived => _lastReceived;

        /// <summary>
        /// Time that this session connected to the server.
        /// </summary>
        public DateTime ConnectedTime { get; private set; }

        /// <summary>
        /// Base socket for this session.
        /// </summary>
        public SocketHandler<TSession, TConfig> SocketHandler { get; private set; }

        /// <summary>
        /// Contains the version number of the protocol used by the other end of the connection.
        /// </summary>
        public byte OtherProtocolVersion { get; private set; }

        private System.Net.Sockets.Socket _socket;

        /// <summary>
        /// Raw socket for this session.
        /// </summary>
        public System.Net.Sockets.Socket Socket => _socket;

        /// <summary>
        /// Reset event used to ensure only one MqWorker can write to the socket at a time.
        /// </summary>
        private SemaphoreSlim _writeSemaphore;

        /// <summary>
        /// This event fires when a connection has been established.
        /// </summary>
        public event EventHandler<SessionEventArgs<TSession, TConfig>> Connected;

        /// <summary>
        /// This event fires when a connection has been shutdown.
        /// </summary>
        public event EventHandler<SessionClosedEventArgs<TSession, TConfig>> Closed;

        /// <summary>
        /// Send partial buffer used to contain the data sent which exceeds the 16 bit alignment.
        /// </summary>
        private readonly byte[] _sendPartialBuffer = new byte[16];

        /// <summary>
        /// Length of the data in the send partial buffer.
        /// </summary>
        private int _sendPartialBufferLength = 0;

        /// <summary>
        /// Receive partial buffer used to contain the data sent which exceeds the 16 bit alignment.
        /// </summary>
        private readonly byte[] _receivePartialBuffer = new byte[16];

        /// <summary>
        /// Length of the data in the receive partial buffer.
        /// </summary>
        private int _receivePartialBufferLength = 0;

        /// <summary>
        /// Pooled buffer segment used for the transformation of data.
        /// </summary>
        private ArraySegment<byte> _receiveTransformedBuffer;

        /// <summary>
        /// Contains state information about the current receiving header.
        /// </summary>
        private readonly Header _receivingHeader = new Header();

        private IMemoryOwner<byte> _readMemoryOwner;

        private Memory<byte> _readMemoryBuffer;

        /// <summary>
        /// Creates a new socket session with a new Id.
        /// </summary>
        protected SocketSession()
        {
            Id = Guid.NewGuid();
            CurrentSocketSessionState = SocketSessionState.Unknown;
        }

        /// <summary>
        /// Sets up this socket with the specified configurations.
        /// </summary>
        /// <param name="args">Args to initialize the socket with.</param>
        public static TSession Create(SocketSessionCreateArguments<TSession, TConfig> args)
        {
            var session = new TSession
            {
                _config = args.SessionConfig,
                _socket = args.SessionSocket,
                _writeSemaphore = new SemaphoreSlim(1, 1),
                SocketHandler = args.SocketHandler,
            };

            if (session._config.SendTimeout > 0)
                session._socket.SendTimeout = session._config.SendTimeout;

            if (session._config.SendAndReceiveBufferSize > 0)
                session._socket.ReceiveBufferSize = session._config.SendAndReceiveBufferSize;

            if (session._config.SendAndReceiveBufferSize > 0)
                session._socket.SendBufferSize = session._config.SendAndReceiveBufferSize;

            try
            {
                session._socket.NoDelay = true;
            }
            catch
            {
                // Some sockets can't change this value.
            }

            session._readMemoryOwner = args.BufferMemoryPool.Rent();
            session._readMemoryBuffer = session._readMemoryOwner.Memory;

            session._socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

            session.OnSetup();

            return session;
        }

        /// <summary>
        /// Start the session's receive events.
        /// </summary>
        void ISetupSocketSession.StartSession()
        {
            if (CurrentSocketSessionState != SocketSessionState.Closed)
                return;

            // Change into a securing state for the 
            CurrentSocketSessionState = SocketSessionState.Securing;
            ConnectedTime = DateTime.UtcNow;

            Task.Factory.StartNew(ReceiveLoop);

            // Send the protocol version number along with the public key to the connected client.
            if (SocketHandler.Mode == SocketMode.Client)
            {
            }
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (true)
                {
                    var read = await _socket.ReceiveAsync(_readMemoryBuffer, SocketFlags.None);
                    if (read == 0)
                    {
                        _config.Logger.ConditionalTrace("Socket received 0 bytes. Exiting receive loop.");
                        break;
                    }

                    await HandleIncomingBytes(_readMemoryBuffer.Slice(0, read));
                }
            }
            catch
            {
                await Close(CloseReason.SocketError);
            }
        }

        /// <summary>
        /// Called after the initial setup has occurred on the session.
        /// </summary>
        protected abstract void OnSetup();

        /// <summary>
        /// Called when this session is connected to the socket.
        /// </summary>
        protected virtual void OnConnected()
        {
            //logger.Info("Session {0}: Connected", Id);
            Connected?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession) this));
        }

        /// <summary>
        /// Called when this session is disconnected from the socket.
        /// </summary>
        /// <param name="reason">Reason this socket is disconnecting</param>
        protected virtual void OnDisconnected(CloseReason reason)
        {
            Closed?.Invoke(this, new SessionClosedEventArgs<TSession, TConfig>((TSession) this, reason));
        }

        /// <summary>
        /// Overridden to parse incoming bytes from the wire.
        /// </summary>
        /// <param name="buffer">Buffer of bytes to parse.</param>
        protected abstract Task<byte> HandleIncomingBytes(Memory<byte> buffer);

        /// <summary>
        /// Asynchronously send raw bytes to the socket and waits until data is sent to the underlying system to send.
        /// </summary>
        /// <param name="buffer">Buffer bytes to send.</param>
        protected async Task Send(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (Socket == null || Socket.Connected == false)
                return;

            if (buffer.Length == 0)
                throw new ArgumentException("Buffer can not be empty.", nameof(buffer));

            var bufferLength = buffer.Length;

            if (bufferLength > _config.SendAndReceiveBufferSize)
            {
                _config.Logger?.Error(
                    $"{SocketHandler.Mode}: Sending {bufferLength} bytes exceeds the SendAndReceiveBufferSize[{_config.SendAndReceiveBufferSize}].");
                throw new Exception(
                    $"Sending {bufferLength} bytes exceeds the SendAndReceiveBufferSize[{_config.SendAndReceiveBufferSize}].");
            }

            _config.Logger?.Trace($"{SocketHandler.Mode}: Waiting for writing semaphore.");

            // Ensure sending occurs sequentially.
            await _writeSemaphore.WaitAsync(-1, cancellationToken);

            _config.Logger?.Trace($"{SocketHandler.Mode}: Passed writing semaphore.");

            var totalSent = 0;
            try
            {
                var sendBuffer = ReadOnlyMemory<byte>.Empty;
                while (totalSent != bufferLength)
                {
                    sendBuffer = totalSent == 0 ? buffer : sendBuffer.Slice(totalSent);
                    totalSent += await Socket.SendAsync(sendBuffer, SocketFlags.None, cancellationToken);
                    _config.Logger?.Trace($"{SocketHandler.Mode}: Sent {sendBuffer} of {bufferLength} bytes.");
                }
            }
            catch (ObjectDisposedException)
            {
                _config.Logger?.Error($"{SocketHandler.Mode}: System can not send data asynchronously.");
                Close(CloseReason.SocketError);
            }
            finally
            {
                _writeSemaphore.Release();
                _config.Logger?.Trace($"{SocketHandler.Mode}: Released writing semaphore.");
            }
        }

        private class Header
        {
            public enum Type : byte
            {
                /// <summary>
                /// Type is unset.
                /// </summary>
                Unknown = 0,

                /// <summary>
                /// The header contains a length of a body payload.
                /// </summary>
                BodyPayload = 1,

                /// <summary>
                /// The header is a single byte consumed for padding purposes.
                /// </summary>
                Padding = 2,
                ConnectionClose = 3,
                EncryptChannel = 4
            }


            public enum State
            {
                Empty,
                ReadingBodyLength,
                ReadingCloseReason,
                ReadingEncryptionKey,
                Complete,
            }

            public State HeaderReceiveState = State.Empty;
            public Type HeaderType;
            public readonly byte[] BodyLengthBuffer = new byte[2];
            public int BodyLengthBufferLength;
            public ushort BodyLength;
            public int BodyPosition;

            public void Reset()
            {
                HeaderType = Type.Unknown;
                HeaderReceiveState = State.Empty;
                BodyLengthBufferLength = 0;
                BodyPosition = 0;
                BodyLength = 0;
                BodyLengthBufferLength = 0;
            }
        }
        
        private bool ReceiveCompleteInternal(byte[] buffer, int offset, int count)
        {
            // Update the last time this session was active to prevent timeout.
            _lastReceived = DateTime.UtcNow;
            var position = 0;
            var receiveBuffer = _receiveTransformedBuffer.Array;
            var receiveOffset = _receiveTransformedBuffer.Offset;


            while (position < receiveLength)
            {
                if (receiveLength == 0)
                    break;

                // See if we are ready for a new header.
                if (_receivingHeader.HeaderReceiveState == Header.State.Empty)
                {
                    _receivingHeader.HeaderType =
                        (Header.Type) receiveBuffer[receiveOffset + position];

                    switch (_receivingHeader.HeaderType)
                    {
                        case Header.Type.BodyPayload:
                            if (CurrentSocketSessionState != SocketSessionState.Connected)
                            {
                                Close(CloseReason.ProtocolError);
                                return false;
                            }

                            _receivingHeader.HeaderReceiveState = Header.State.ReadingBodyLength;
                            break;

                        case Header.Type.ConnectionClose:
                            _receivingHeader.HeaderReceiveState = Header.State.ReadingCloseReason;
                            break;

                        case Header.Type.EncryptChannel:
                            if (CurrentSocketSessionState != SocketSessionState.Securing)
                            {
                                Close(CloseReason.ProtocolError);
                                return false;
                            }

                            _receivingHeader.HeaderReceiveState = Header.State.ReadingEncryptionKey;
                            break;

                        case Header.Type.Padding:
                            position++;
                            continue;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // Advance the position past the record type bit.
                    position++;

                    _config.Logger?.Trace($"{SocketHandler.Mode}: New header {_receivingHeader.HeaderType}");
                }

                // Read the close reason.
                if (_receivingHeader.HeaderReceiveState == Header.State.ReadingCloseReason
                    && position < receiveLength)
                {
                    var reason =
                        (CloseReason) receiveBuffer[receiveOffset + position];

                    // Close the session.
                    Close(reason);
                    return false;
                }

                // Read the DH key
                if (_receivingHeader.HeaderReceiveState == Header.State.ReadingEncryptionKey
                    && position < receiveLength)
                {
                    var readLength = Math.Min(140 - (int) _negotiationStream.Length, receiveLength - position);
                    var encryptionKeyBuffer = new byte[readLength];
                    Buffer.BlockCopy(receiveBuffer, receiveOffset + position, encryptionKeyBuffer, 0,
                        readLength);

                    position += readLength;
                }

                // Read the number of bytes contained in the body.
                if (_receivingHeader.HeaderReceiveState == Header.State.ReadingBodyLength
                    && position < receiveLength)
                {
                    // See if the buffer has any contents.
                    if (_receivingHeader.BodyLengthBufferLength == 0)
                    {
                        if (position + 1 < count) // See if we can read the entire size at once.
                        {
                            _receivingHeader.BodyLength = BitConverter.ToUInt16(receiveBuffer,
                                receiveOffset + position);
                            position += 2;

                            // Body length complete.
                            _receivingHeader.HeaderReceiveState = Header.State.Complete;
                        }
                        else
                        {
                            // Read the first byte of the body length.
                            _receivingHeader.BodyLengthBuffer[0] =
                                receiveBuffer[receiveOffset + position];
                            _receivingHeader.BodyLengthBufferLength = 1;
                            // Nothing more to read.
                            break;
                        }
                    }
                    else
                    {
                        // The buffer already contains a byte.
                        _receivingHeader.BodyLengthBuffer[1] =
                            receiveBuffer[receiveOffset + position];
                        position++;

                        _receivingHeader.BodyLength = BitConverter.ToUInt16(_receivingHeader.BodyLengthBuffer, 0);

                        // Body length complete.
                        _receivingHeader.HeaderReceiveState = Header.State.Complete;
                    }
                }

                // If we do not have a complete receive header, stop parsing
                if (_receivingHeader.HeaderReceiveState != Header.State.Complete)
                    break;

                // Reset the receive header buffer info.
                _receivingHeader.BodyLengthBufferLength = 0;

                var currentMessageReadLength = Math.Min(_receivingHeader.BodyLength - _receivingHeader.BodyPosition,
                    receiveLength - position);

                if (currentMessageReadLength == 0)
                    break;

                _config.Logger?.Trace($"{SocketHandler.Mode}: Received {currentMessageReadLength} decrypted bytes.");

                var readBuffer = new byte[currentMessageReadLength];
                Buffer.BlockCopy(receiveBuffer, receiveOffset + position,
                    readBuffer, 0, currentMessageReadLength);

                _receivingHeader.BodyPosition += currentMessageReadLength;
                position += currentMessageReadLength;

                _config.Logger?.Trace($"{SocketHandler.Mode}: Read {readBuffer.Length} body bytes.");

                HandleIncomingBytes(readBuffer);

                if (_receivingHeader.BodyPosition == _receivingHeader.BodyLength)
                {
                    _receivingHeader.Reset();
                }
            }

            return true;
        }

        /// <summary>
        /// Called when this session is desired or requested to be closed.
        /// </summary>
        /// <param name="reason">Reason this socket is closing.</param>
        public virtual Task Close(CloseReason reason)
        {
            // If this session has already been closed, nothing more to do.
            if (CurrentSocketSessionState == SocketSessionState.Closed && reason != CloseReason.ConnectionRefused)
                return Task.CompletedTask;

            _config.Logger?.Trace($"{SocketHandler.Mode}: Connection closed. Reason: {reason}.");

            CurrentSocketSessionState = SocketSessionState.Closed;

            // close the socket associated with the client
            try
            {
                if (Socket.Connected)
                {
                    Socket.Shutdown(SocketShutdown.Receive);
                    Socket.Disconnect(false);
                }
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                Socket.Close(1000);
            }

            _readMemoryOwner.Dispose();
            _readMemoryOwner = null;
            _readMemoryBuffer = null;

            CurrentSocketSessionState = SocketSessionState.Closed;

            // Notify the session has been closed.
            OnDisconnected(reason);
        }

        /// <summary>
        /// String representation of the active session.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            return $"{SocketHandler.Mode} TcpSocketSession;";
        }

        /// <summary>
        /// Disconnects client and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (CurrentSocketSessionState == SocketSessionState.Connected)
                Close(CloseReason.Closing);
        }
    }
}