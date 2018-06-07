using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.TcpSocket
{
    /// <summary>
    /// Base socket session to be sub-classes by the implementer.
    /// </summary>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    /// <typeparam name="TSession">Session for this connection.</typeparam>
    public abstract class TcpSocketSession<TSession, TConfig> : IDisposable, ISecureSocketSession
        where TSession : TcpSocketSession<TSession, TConfig>, new()
        where TConfig : TcpSocketConfig
    {
        /// <summary>
        /// Current state of the socket.
        /// </summary>
        public enum State : byte
        {
            /// <summary>
            /// State has not been set.
            /// </summary>
            Unknown,

            /// <summary>
            ///  Session has initiated a connection request and is negotiating a secured channel of communication.
            /// </summary>
            Securing,

            /// <summary>
            /// Session has connected to remote session.
            /// </summary>
            Connected,

            /// <summary>
            /// Session has been closed and no longer can be used.
            /// </summary>
            Closed,

            /// <summary>
            /// Socket is in an error state.
            /// </summary>
            Error,

        }

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
        public State CurrentState { get; protected set; }

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
        public TcpSocketHandler<TSession, TConfig> SocketHandler { get; private set; }

        /// <summary>
        /// Contains the version number of the protocol used by the other end of the connection.
        /// </summary>
        public byte ConnectionProtocolVersion { get; private set; }

        /// <summary>
        /// Processor to handle all inbound messages.
        /// </summary>
        protected ActionProcessor<Guid> InboxProcessor;

        /// <summary>
        /// Processor to handle all outbound messages.
        /// </summary>
        protected ActionProcessor<Guid> OutboxProcessor;


        private System.Net.Sockets.Socket _socket;

        /// <summary>
        /// Raw socket for this session.
        /// </summary>
        public System.Net.Sockets.Socket Socket => _socket;

        /// <summary>
        /// Async args used to send data to the wire.
        /// </summary>
        private SocketAsyncEventArgs _sendArgs;

        /// <summary>
        /// Async args used to receive data off the wire.
        /// </summary>
        private SocketAsyncEventArgs _receiveArgs;

        /// <summary>
        /// Pool used by all the sessions on this SessionHandler.
        /// </summary>
        private SocketAsyncEventArgsManager _argsPool;

        /// <summary>
        /// Reset event used to ensure only one MqWorker can write to the socket at a time.
        /// </summary>
        private SemaphoreSlim _writeSemaphore;


        /// <summary>
        /// Cache for commonly called methods used throughout the session.
        /// </summary>
        public ServiceMethodCache ServiceMethodCache;

        /// <summary>
        /// This event fires when a connection has been established.
        /// </summary>
        public event EventHandler<SessionEventArgs<TSession, TConfig>> Connected;

        /// <summary>
        /// This event fires when a connection has been shutdown.
        /// </summary>
        public event EventHandler<SessionClosedEventArgs<TSession, TConfig>> Closed;

        private MemoryQueueStream _negotiationStream;

        private byte[] _sendBuffer = new byte[16];
        private int _sendBufferLength = 0;

        private byte[] _receiveTransformedBuffer;
        private byte[] _receivePartialBuffer = new byte[16];
        private int _receivePartialBufferLength = 0;

        private RSACng _rsa;

        /// <summary>
        /// Contains the encryption and description methods.
        /// </summary>
        private Aes _aes;

        private ICryptoTransform _decryptor;

        private ICryptoTransform _encryptor;

        private ReceiveHeader receiveHeader = new ReceiveHeader();
        private static byte[][] paddingBuffer;

        /// <summary>
        /// Creates a new socket session with a new Id.
        /// </summary>
        protected TcpSocketSession()
        {
            Id = Guid.NewGuid();
            CurrentState = State.Closed;
            _negotiationStream = new MemoryQueueStream();

            var paddingBufferList = new byte[15][];
            if (paddingBuffer == null)
            {
                for (int i = 0; i < 15; i++)
                {
                    paddingBufferList[i] = new byte[i + 1];
                    for (int j = 0; j < i + 1; j++)
                        paddingBufferList[i][j] = (byte) ReceiveHeader.Type.Padding;
                }
                paddingBuffer = paddingBufferList;
            }
        }

        /// <summary>
        /// Sets up this socket with the specified configurations.
        /// </summary>
        /// <param name="args">Args to initialize the socket with.</param>
        public static TSession Create(TlsSocketSessionCreateArguments<TSession, TConfig> args)
        {
            var session = new TSession
            {
                _config = args.SessionConfig,
                _argsPool = args.SocketArgsManager,
                _socket = args.SessionSocket,
                _writeSemaphore = new SemaphoreSlim(1, 1),
                SocketHandler = args.TlsSocketHandler,
                _sendArgs = args.SocketArgsManager.Create(),
                _receiveArgs = args.SocketArgsManager.Create(),
                InboxProcessor = args.InboxProcessor,
                OutboxProcessor = args.OutboxProcessor,
                ServiceMethodCache = args.ServiceMethodCache
            };

            session._receiveTransformedBuffer = new byte[args.SessionConfig.SendAndReceiveBufferSize];
            session._sendArgs.Completed += session.IoCompleted;
            session._receiveArgs.Completed += session.IoCompleted;

            if (session._config.SendTimeout > 0)
                session._socket.SendTimeout = session._config.SendTimeout;

            if (session._config.SendAndReceiveBufferSize > 0)
                session._socket.ReceiveBufferSize = session._config.SendAndReceiveBufferSize;

            if (session._config.SendAndReceiveBufferSize > 0)
                session._socket.SendBufferSize = session._config.SendAndReceiveBufferSize;

            session._socket.NoDelay = true;
            session._socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

            session.OnSetup();

            return session;
        }

        /// <summary>
        /// Start the session's receive events.
        /// </summary>
        void ISecureSocketSession.SecureSession(RSACng rsa)
        {
            if (CurrentState != State.Closed)
                return;

            _rsa = rsa;
            CurrentState = State.Securing;
            ConnectedTime = DateTime.UtcNow;

            // Start receiving data for the secured channel.
            _socket.ReceiveAsync(_receiveArgs);

            // Send the protocol version number along with the public key to the connected client.
            if (SocketHandler.Mode == TcpSocketMode.Server)
            {
                var versionPublicKey = new byte[513];
                versionPublicKey[0] = ProtocolVersion;
                Buffer.BlockCopy(SocketHandler.RsaPublicKey, 0, versionPublicKey, 1, 512);

                Send(versionPublicKey, 0, versionPublicKey.Length);
            }
            
        }


        private void SecureConnectionReceive(byte[] buffer)
        {
            if (CurrentState == State.Closed)
                return;

            _negotiationStream.Write(buffer);

            if (SocketHandler.Mode == TcpSocketMode.Client)
            {
                // Set the connection version number.
                if (ConnectionProtocolVersion == 0)
                {
                    var version = new byte[1];
                    _negotiationStream.Read(version, 0, 1);
                    ConnectionProtocolVersion = version[0];
                }

                // 4096 bits for the public key
                if (ConnectionProtocolVersion > 0
                    && SocketHandler.RsaPublicKey == null
                    && _negotiationStream.Length >= 512)
                {
                    SocketHandler.RsaPublicKey = new byte[512];
                    _negotiationStream.Read(SocketHandler.RsaPublicKey, 0, 512);

                    _rsa = new RSACng();


                    _rsa.ImportParameters(new RSAParameters
                    {
                        Modulus = SocketHandler.RsaPublicKey,
                        Exponent = new byte[] {1, 0, 1}
                    });

                    // Setup the 256 AES encryption
                    _aes = new AesCryptoServiceProvider
                    {
                        KeySize = 256,
                        Mode = CipherMode.CBC,
                        Padding = PaddingMode.PKCS7
                    };

                    // Server side.
                    _aes.GenerateKey();
                    _aes.GenerateIV();

                    var ivKey = new byte[48];

                    // Copy the arrays into the a structure to be read by the server.
                    Buffer.BlockCopy(_aes.IV, 0, ivKey, 0, 16);
                    Buffer.BlockCopy(_aes.Key, 0, ivKey, 16, 32);

                    var rsaIvKey = _rsa.Encrypt(ivKey, RSAEncryptionPadding.OaepSHA512);

                    _rsa.Dispose();
                    _rsa = null;

                    // Send the encryption key.
                    Send(rsaIvKey, 0, rsaIvKey.Length);

                    SecureConnectionComplete();
                }
            }
            else // Server
            {

                if (_aes == null
                    && _negotiationStream.Length >= 512)
                {
                    var rsaIvKey = new byte[512];
                    _negotiationStream.Read(rsaIvKey, 0, rsaIvKey.Length);
                    var ivKey = _rsa.Decrypt(rsaIvKey, RSAEncryptionPadding.OaepSHA512);

                    var iv = new byte[16];
                    var key = new byte[32];

                    Buffer.BlockCopy(ivKey, 0, iv, 0, 16);
                    Buffer.BlockCopy(ivKey, 16, key, 0, 32);

                    // Setup the 256 AES encryption
                    _aes = new AesCryptoServiceProvider
                    {
                        KeySize = 256,
                        Mode = CipherMode.CBC,
                        Padding = PaddingMode.PKCS7,
                        Key = key,
                        IV = iv
                    };

                    SecureConnectionComplete();
                }
            }
        }

        private void SecureConnectionComplete()
        {
            // If there is left over data in the stream, send it on to the 
            if (_negotiationStream.Length > 0)
            {
                var excessBuffer = new byte[_negotiationStream.Length];
                _negotiationStream.Read(excessBuffer, 0, excessBuffer.Length);

                HandleIncomingBytes(excessBuffer);
            }

            // Set the encryptor and decryptor.
            _decryptor = _aes.CreateDecryptor();
            _encryptor = _aes.CreateEncryptor(); 

            _negotiationStream.Close();
            _negotiationStream = null;

            // Set the state to connected.
            CurrentState = State.Connected;
            OnConnected();
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
            Connected?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession)this));
        }

        /// <summary>
        /// Called when this session is disconnected from the socket.
        /// </summary>
        /// <param name="reason">Reason this socket is disconnecting</param>
        protected virtual void OnDisconnected(CloseReason reason)
        {
            Closed?.Invoke(this, new SessionClosedEventArgs<TSession, TConfig>((TSession)this, reason));
        }

        /// <summary>
        /// Overridden to parse incoming bytes from the wire.
        /// </summary>
        /// <param name="buffer">Buffer of bytes to parse.</param>
        protected abstract void HandleIncomingBytes(byte[] buffer);

        /// <summary>
        /// This method is called whenever a receive or send operation is completed on a socket 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
        protected virtual void IoCompleted(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Disconnect:
                    Close(CloseReason.Closing);
                    break;

                case SocketAsyncOperation.Receive:
                    RecieveComplete(e);

                    break;

                case SocketAsyncOperation.Send:
                    SendComplete(e);
                    break;

                default:
                    throw new ArgumentException(
                        "The last operation completed on the socket was not a receive, send connect or disconnect.");
            }
        }


        private short TransformDataBuffer(byte[] bufferSource, int offsetSource, int lengthSource, byte[] bufferDest, int offsetDest, byte[] transformBuffer, ref int transformBufferLength, ICryptoTransform transformer, bool preDebug, bool postDebug)
        {
            int transformLength = 0;
            if (transformBufferLength > 0)
            {
                int bufferTaken = Math.Min(lengthSource, 16 - transformBufferLength);
                Buffer.BlockCopy(bufferSource, offsetSource, transformBuffer, transformBufferLength, bufferTaken);
                transformBufferLength += bufferTaken;
                offsetSource += bufferTaken;
                lengthSource -= bufferTaken;
            }

            if (transformBufferLength == 16)
            {
                transformLength += 16;
                transformer.TransformBlock(transformBuffer, 0, transformBufferLength, bufferDest, offsetDest);
                if (preDebug)
                {
                    WriteBuffer("Pre 16 Byte Buffer", transformBuffer, 0, transformBufferLength);
                }

                if (postDebug)
                {
                    WriteBuffer("Post 16 Byte Buffer", bufferDest, offsetDest, 16);
                }
            }
            else if (lengthSource == 0)
            {
                return 0;
            }

            // Get the size of the block in chucks of 16 bytes.
            int blockTransformLength = lengthSource / 16 * 16;
            int transformRemain = lengthSource - blockTransformLength;

            if (preDebug && blockTransformLength > 0)
            {
                WriteBuffer("Pre block transform", bufferSource, offsetSource, blockTransformLength);
            }

            // Only transform if we have a block large enough.
            if(blockTransformLength > 0)
                transformLength += transformer.TransformBlock(bufferSource, offsetSource, blockTransformLength, bufferDest,
                    offsetDest + transformBufferLength);

            if (postDebug && blockTransformLength > 0)
            {
                WriteBuffer("Post block transform", bufferDest, offsetDest + transformBufferLength, blockTransformLength);
            }

            // If the buffer was used this round, reset it to zero.
            if (transformBufferLength == 16)
                transformBufferLength = 0;


            if (postDebug)
            {
                WriteBuffer("Whole buffer", bufferDest, offsetDest, transformLength);
            }

            if (transformRemain > 0)
            {
                Buffer.BlockCopy(bufferSource, offsetSource + blockTransformLength, transformBuffer, 0, transformRemain);
                transformBufferLength = transformRemain;
            }

            return (short)transformLength;
        }

     
        /// <summary>
        /// Sends raw bytes to the socket.  Blocks until data is sent to the underlying system to send.
        /// Before transport encryption has been established, any buffer size will be sent.
        /// After transport encryption has been established, only buffers in increments of 16.
        /// Excess will be buffered until the next write.
        /// </summary>
        /// <param name="buffer">Buffer bytes to send.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="length">Total bytes to send.</param>
        protected virtual void Send(byte[] buffer, int offset, int length)
        {
            if (Socket == null || Socket.Connected == false)
                return;

            if(length + 3 >= Config.SendAndReceiveBufferSize)
                throw new ArgumentException("Too large");

            _writeSemaphore.Wait(-1);

            if (_encryptor != null)
            {
                var lengthBytes = BitConverter.GetBytes(length);
                byte[] header = {
                    (byte) ReceiveHeader.Type.FullMessage,
                    lengthBytes[0],
                    lengthBytes[1]
                };

                // Encrypt the header.
                var sendLength = TransformDataBuffer(header, 0, 3, _sendArgs.Buffer, _sendArgs.Offset, _sendBuffer,
                    ref _sendBufferLength, _encryptor, false, false);


                // Encrypt the message.
                sendLength += TransformDataBuffer(buffer, offset, length, _sendArgs.Buffer, _sendArgs.Offset + sendLength, _sendBuffer,
                    ref _sendBufferLength, _encryptor, false, false);

                if (sendLength == 0)
                    return;

                _sendArgs.SetBuffer(_sendArgs.Offset, sendLength);

                SendInternal(null, 0, 0);
            }
            else
            {
                SendInternal(buffer, offset, length);
            }
        }

        /// <summary>
        /// Send out any buffered data to the connection.
        /// </summary>
        public void Flush()
        {
            if (Socket == null || Socket.Connected == false)
                return;

            _writeSemaphore.Wait(-1);

            if (_sendBufferLength == 0)
                return;

            var paddingLength = 16 - _sendBufferLength;

            // Padding
            var sendLength = TransformDataBuffer(paddingBuffer[paddingLength - 1], 0, paddingLength, _sendArgs.Buffer,
                _sendArgs.Offset, _sendBuffer,
                ref _sendBufferLength, _encryptor, false, false);

            _sendArgs.SetBuffer(_sendArgs.Offset, sendLength);

            SendInternal(null, 0, 0);

        }




        /// <summary>
        /// Sends raw bytes to the socket.  Blocks until data is sent to the underlying system to send.
        /// Before transport encryption has been established, any buffer size will be sent.
        /// After transport encryption has been established, only buffers in increments of 16.
        /// Excess will be buffered until the next write.
        /// </summary>
        /// <param name="buffer">Buffer bytes to send.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="length">Total bytes to send.</param>
        private void SendInternal(byte[] buffer, int offset, int length)
        {
            if (Socket == null || Socket.Connected == false)
                return;

            if (length > Config.SendAndReceiveBufferSize)
                throw new ArgumentException("Too large");

            if (buffer != null)
            {
                Buffer.BlockCopy(buffer, offset, _sendArgs.Buffer, _sendArgs.Offset, length);

                // Update the buffer length.
                _sendArgs.SetBuffer(_sendArgs.Offset, length);
            }

            try
            {
                if (Socket.SendAsync(_sendArgs) == false)
                {
                    IoCompleted(this, _sendArgs);
                }
            }
            catch (ObjectDisposedException)
            {
                Close(CloseReason.SocketError);
            }
        }

        /// <summary>
        /// This method is invoked when an asynchronous send operation completes.  
        /// The method issues another receive on the socket to read any additional data sent from the client
        /// </summary>
        /// <param name="e">Event args of this action.</param>
        private void SendComplete(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Close(CloseReason.SocketError);
            }
            _writeSemaphore.Release(1);
        }



        private class ReceiveHeader
        {
            public enum Type : byte
            {
                Unknown = 0,
                FullMessage = 1,
                Padding = 2,
            }

            public enum State
            {
                Empty,
                ReadingBodyLength,
                Complete,
            }

            public State HeaderReceiveState = State.Empty;
            public Type HeaderType;
            public readonly byte[] BodyLengthBuffer = new byte[2];
            public int BodyLengthBufferLength;
            public short BodyLength;
            public int BodyPosition;


        }

        /// <summary>
        /// This method is invoked when an asynchronous receive operation completes. 
        /// If the remote host closed the connection, then the socket is closed.
        /// </summary>
        /// <param name="e">Event args of this action.</param>
        protected void RecieveComplete(SocketAsyncEventArgs e)
        {
            if (CurrentState == State.Closed)
                return;

            if (e.BytesTransferred == 0)
            {
                HandleIncomingBytes(null);
                return;
            }

            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                // Update the last time this session was active to prevent timeout.
                _lastReceived = DateTime.UtcNow;

                // Create a copy of these bytes.
                byte[] buffer;

                if (_decryptor != null)
                {
                    var position = 0;

                    int receiveLength = TransformDataBuffer(e.Buffer, e.Offset, e.BytesTransferred,
                        _receiveTransformedBuffer, 0, _receivePartialBuffer,
                        ref _receivePartialBufferLength, _decryptor, false, false);

                    while (position < receiveLength)
                    {
                        if (receiveLength == 0)
                            break;

                        // See if we are ready for a new header.
                        if (receiveHeader.HeaderReceiveState == ReceiveHeader.State.Empty)
                        {
                            receiveHeader.HeaderType = (ReceiveHeader.Type) _receiveTransformedBuffer[position];

                            switch (receiveHeader.HeaderType)
                            {
                                case ReceiveHeader.Type.FullMessage:
                                    receiveHeader.HeaderReceiveState = ReceiveHeader.State.ReadingBodyLength;
                                    break;
                                case ReceiveHeader.Type.Padding:
                                    position++;
                                    continue;
                                case ReceiveHeader.Type.Unknown:
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            position++;
                        }

                        if (receiveHeader.HeaderReceiveState == ReceiveHeader.State.ReadingBodyLength
                            && position < receiveLength)
                        {
                            // See if the buffer has any contents.
                            if (receiveHeader.BodyLengthBufferLength == 0)
                            {
                                if (position + 1 < e.BytesTransferred) // See if we can read the entire size at once.
                                {
                                    receiveHeader.BodyLength = BitConverter.ToInt16(_receiveTransformedBuffer, position);
                                    position += 2;

                                    // Body length complete.
                                    receiveHeader.HeaderReceiveState = ReceiveHeader.State.Complete;
                                }
                                else
                                {
                                    // Read the first byte of the body length.
                                    receiveHeader.BodyLengthBuffer[0] = _receiveTransformedBuffer[position];
                                    receiveHeader.BodyLengthBufferLength = 1;
                                    // Nothing more to read.
                                    break;
                                }
                            }
                            else
                            {
                                // The buffer already contains a byte.
                                receiveHeader.BodyLengthBuffer[1] = _receiveTransformedBuffer[position];
                                position++;

                                receiveHeader.BodyLength = BitConverter.ToInt16(receiveHeader.BodyLengthBuffer, 0);

                                // Body length complete.
                                receiveHeader.HeaderReceiveState = ReceiveHeader.State.Complete;
                            }
                        }

                        // If we do not have a complete receive header, stop parsing
                        if (receiveHeader.HeaderReceiveState != ReceiveHeader.State.Complete)
                            break;

                        // Reset the receive header buffer info.
                        receiveHeader.BodyLengthBufferLength = 0;

                        var currentMessageReadLength = Math.Min(receiveHeader.BodyLength - receiveHeader.BodyPosition, receiveLength - position);

                        if (currentMessageReadLength == 0)
                            break;

                        buffer = new byte[currentMessageReadLength];
                        Buffer.BlockCopy(_receiveTransformedBuffer, position, buffer, 0, currentMessageReadLength);

                        receiveHeader.BodyPosition += currentMessageReadLength;
                        position += currentMessageReadLength;

                        HandleIncomingBytes(buffer);

                        if (receiveHeader.BodyPosition == receiveHeader.BodyLength)
                        {
                            receiveHeader.HeaderReceiveState = ReceiveHeader.State.Empty;
                            receiveHeader.BodyPosition = 0;
                        }
                    }
                }
                else
                {
                    buffer = new byte[_receiveArgs.BytesTransferred];
                    Buffer.BlockCopy(_receiveArgs.Buffer, _receiveArgs.Offset, buffer, 0,
                        _receiveArgs.BytesTransferred);
                    SecureConnectionReceive(buffer);
                }

                try
                {
                    // Re-setup the receive async call.
                    if (Socket.ReceiveAsync(e) == false)
                    {
                        IoCompleted(this, e);
                    }
                }
                catch (ObjectDisposedException)
                {
                    Close(CloseReason.SocketError);
                }
            }
            else
            {
                Close(CloseReason.SocketError);
            }
        }

        private void WriteBuffer(string header, byte[] buffer, int offset, int length)
        {
            var sb = new StringBuilder(header + " new byte[" + length + "] { ");
            for (var i = 0; i < length; i++)
            {
                sb.Append(buffer[i + offset]).Append(" ");
            }
            sb.AppendLine("}");
            Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Called when this session is desired or requested to be closed.
        /// </summary>
        /// <param name="reason">Reason this socket is closing.</param>
        public virtual void Close(CloseReason reason)
        {
            // If this session has already been closed, nothing more to do.
            if (CurrentState == State.Closed && reason != CloseReason.ConnectionRefused)
                return;

            CurrentState = State.Closed;

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

            _sendArgs.Completed -= IoCompleted;
            _receiveArgs.Completed -= IoCompleted;

            // Free the SocketAsyncEventArg so they can be reused by another client
            _argsPool.Free(_sendArgs);
            _argsPool.Free(_receiveArgs);

            InboxProcessor.Deregister(Id);
            OutboxProcessor.Deregister(Id);

            CurrentState = State.Closed;

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
            if (CurrentState == State.Connected)
                Close(CloseReason.Closing);

        }
    }
}