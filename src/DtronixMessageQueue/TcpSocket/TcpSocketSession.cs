﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    public abstract class TcpSocketSession<TSession, TConfig> : IDisposable, ISetupSocketSession
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
            /// Session is in the process of securing the communication channel.
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
        public byte OtherProtocolVersion { get; private set; }

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

        private byte[] _sendPartialBuffer = new byte[16];
        private int _sendPartialBufferLength = 0;
        private byte[] _receivePartialBuffer = new byte[16];
        private int _receivePartialBufferLength = 0;


        private ArraySegment<byte> _receiveTransformedBuffer;
        private BufferManager _receiveTransformedBufferManager;


        /// <summary>
        /// Contains the encryption and description methods.
        /// </summary>
        private Aes _aes;

        private ICryptoTransform _decryptor;

        private ICryptoTransform _encryptor;

        private Header _header = new Header();

        private static byte[][] paddingBuffer;

        private ECDiffieHellmanCng dh;

        /// <summary>
        /// Creates a new socket session with a new Id.
        /// </summary>
        protected TcpSocketSession()
        {
            Id = Guid.NewGuid();
            CurrentState = State.Closed;
            _negotiationStream = new MemoryQueueStream();

            dh = new ECDiffieHellmanCng
            {
                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                HashAlgorithm = CngAlgorithm.Sha384
            };

            _decryptor = new PlainCryptoTransform();
            _encryptor = new PlainCryptoTransform();

            var paddingBufferList = new byte[15][];
            if (paddingBuffer == null)
            {
                for (int i = 0; i < 15; i++)
                {
                    paddingBufferList[i] = new byte[i + 1];
                    for (int j = 0; j < i + 1; j++)
                        paddingBufferList[i][j] = (byte) Header.Type.Padding;
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
                ServiceMethodCache = args.ServiceMethodCache,
                _receiveTransformedBufferManager = args.ReceiveBufferManager,
                _receiveTransformedBuffer = args.ReceiveBufferManager.GetBuffer()
            };

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
        void ISetupSocketSession.StartSession()
        {
            if (CurrentState != State.Closed)
                return;
            CurrentState = State.Securing;
            ConnectedTime = DateTime.UtcNow;

            // Start receiving data for the secured channel.
            _socket.ReceiveAsync(_receiveArgs);

            // Send the protocol version number along with the public key to the connected client.
            if (SocketHandler.Mode == TcpSocketMode.Client)
            {
                var key = dh.PublicKey.ToByteArray();
                SendWithHeader(key, 0, key.Length, Header.Type.EncryptChannel, null, 0, 0);
            }
            
        }

        private byte[] _otherDHPublicKey;

        private bool SecureConnectionReceive(byte[] buffer)
        {
            if (CurrentState == State.Closed)
                return false;

            if (CurrentState != State.Securing)
            {
                Close(CloseReason.ProtocolError);
                return false;
            }

            _negotiationStream.Write(buffer);

            // Set the connection version number.

            if (_aes == null
                && _negotiationStream.Length >= 140)
            {
                _otherDHPublicKey = new byte[140];
                _negotiationStream.Read(_otherDHPublicKey, 0, 140);

                var otherPublicKey =
                    ECDiffieHellmanCngPublicKey.FromByteArray(_otherDHPublicKey, CngKeyBlobFormat.EccPublicBlob);
                var generatedKey = dh.DeriveKeyMaterial(otherPublicKey);

                var iv = new byte[16];
                var key = new byte[32];
                // Copy the arrays into the a structure to be read by the server.
                Buffer.BlockCopy(generatedKey, 0, iv, 0, 16);
                Buffer.BlockCopy(generatedKey, 16, key, 0, 32);

                // Setup the 256 AES encryption
                _aes = new AesCryptoServiceProvider
                {
                    KeySize = 256,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.None,
                    Key = key,
                    IV = iv
                };

                if (SocketHandler.Mode == TcpSocketMode.Server)
                {
                    var pKey = dh.PublicKey.ToByteArray();
                    SendWithHeader(pKey, 0, pKey.Length, Header.Type.EncryptChannel, null, 0, 0);
                }


                SecureConnectionComplete();
                return true;
            }

            return false;
        }



        private void SecureConnectionComplete()
        {
            // Set the encryptor and decryptor.
            _decryptor = _aes.CreateDecryptor();
            _encryptor = _aes.CreateEncryptor();

            // If the stream has any extra data sent, pass it along to the reader.
            if (_negotiationStream.Length > 0)
            {
                byte[] partialBuffer = new byte[_negotiationStream.Length];
                _negotiationStream.Read(partialBuffer, 0, partialBuffer.Length);

                ReceiveCompleteInternal(partialBuffer, 0, partialBuffer.Length);
            }

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


        private short TransformDataBuffer(byte[] bufferSource, int offsetSource, int lengthSource, byte[] bufferDest, int offsetDest, byte[] transformBuffer, ref int transformBufferLength, ICryptoTransform transformer)
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
            }
            else if (lengthSource == 0)
                return 0;

            // Get the size of the block in chucks of 16 bytes.
            int blockTransformLength = lengthSource / 16 * 16;
            int transformRemain = lengthSource - blockTransformLength;
            // Only transform if we have a block large enough.
            if(blockTransformLength > 0)
                transformLength += transformer.TransformBlock(bufferSource, offsetSource, blockTransformLength, bufferDest,
                    offsetDest + transformBufferLength);

            // If the buffer was used this round, reset it to zero.
            if (transformBufferLength == 16)
                transformBufferLength = 0;

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
        /// <param name="count">Total bytes to send.</param>
        protected virtual void Send(byte[] buffer, int offset, int count)
        {
            if (Socket == null || Socket.Connected == false)
                return;

            var lengthBytes = BitConverter.GetBytes(count);
            byte[] bodyLengthBytes = {
                lengthBytes[0],
                lengthBytes[1]
            };

            SendWithHeader(buffer, offset, count, Header.Type.FullMessage, bodyLengthBytes, 0, 2);
        }

        /// <summary>
        /// Sends raw bytes to the socket.  Blocks until data is sent to the underlying system to send.
        /// Before transport encryption has been established, any buffer size will be sent.
        /// After transport encryption has been established, only buffers in increments of 16.
        /// Excess will be buffered until the next write.
        /// </summary>
        /// <param name="buffer">Buffer bytes to send.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="count">Total bytes to send.</param>
        /// <param name="type"></param>
        /// <param name="headerBuffer"></param>
        /// <param name="headerOffset"></param>
        /// <param name="headerCount"></param>
        private void SendWithHeader(
            byte[] buffer, 
            int offset, 
            int count, 
            Header.Type type, 
            byte[] headerBuffer,
            int headerOffset, 
            int headerCount)
        {
            if (Socket == null || Socket.Connected == false)
                return;

            if (count > Config.SendAndReceiveBufferSize)
                throw new ArgumentException("Too large");

            _writeSemaphore.Wait(-1);


            var sendLength = TransformDataBuffer(new[] {(byte) type}, 0, 1, _sendArgs.Buffer, _sendArgs.Offset,
                _sendPartialBuffer,
                ref _sendPartialBufferLength, _encryptor);

            if (headerBuffer != null)
            {
                sendLength += TransformDataBuffer(headerBuffer, headerOffset, headerCount, _sendArgs.Buffer,
                    _sendArgs.Offset, _sendPartialBuffer,
                    ref _sendPartialBufferLength, _encryptor);
            }

            if (buffer != null)
            {
                // Encrypt the message.
                sendLength += TransformDataBuffer(buffer, offset, count, _sendArgs.Buffer,
                    _sendArgs.Offset + sendLength, _sendPartialBuffer,
                    ref _sendPartialBufferLength, _encryptor);
            }

            // If this is the last frame in a packet, pad the ending.
            // 1 is for the header type byte.
            if (count + headerCount + 1 != sendLength)
                sendLength += PadSendBuffer(sendLength);

            if (sendLength == 0)
                return;

            _sendArgs.SetBuffer(_sendArgs.Offset, sendLength);

            SendInternal(null, 0, 0);
        }

        private short PadSendBuffer(int currentSendLength)
        {
            var paddingLength = 16 - _sendPartialBufferLength;
            return TransformDataBuffer(paddingBuffer[paddingLength - 1], 0, paddingLength,
                _sendArgs.Buffer,
                _sendArgs.Offset + currentSendLength, _sendPartialBuffer,
                ref _sendPartialBufferLength, _encryptor);
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



        private class Header
        {
            public enum Type : byte
            {
                Unknown = 0,
                FullMessage = 1,
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
            public short BodyLength;
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
                // If the session was closed curing the internal receive, don't read any more.
                if (!ReceiveCompleteInternal(e.Buffer, e.Offset, e.BytesTransferred))
                    return;

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

        private bool ReceiveCompleteInternal(byte[] buffer, int offset, int count)
        {
            // Update the last time this session was active to prevent timeout.
            _lastReceived = DateTime.UtcNow;
            var position = 0;
            var receiveBuffer = _receiveTransformedBuffer.Array;
            var receiveOffset = _receiveTransformedBuffer.Offset;

            int receiveLength = TransformDataBuffer(buffer, offset, count,
                receiveBuffer, receiveOffset, _receivePartialBuffer,
                ref _receivePartialBufferLength, _decryptor);

            while (position < receiveLength)
            {
                if (receiveLength == 0)
                    break;

                // See if we are ready for a new header.
                if (_header.HeaderReceiveState == Header.State.Empty)
                {
                    _header.HeaderType =
                        (Header.Type) receiveBuffer[receiveOffset + position];

                    switch (_header.HeaderType)
                    {
                        case Header.Type.FullMessage:
                            if (CurrentState != State.Connected)
                            {
                                Close(CloseReason.ProtocolError);
                                return false;
                            }
                            _header.HeaderReceiveState = Header.State.ReadingBodyLength;
                            break;

                        case Header.Type.ConnectionClose:
                            _header.HeaderReceiveState = Header.State.ReadingCloseReason;
                            break;

                        case Header.Type.EncryptChannel:
                            if (CurrentState != State.Securing)
                            {
                                Close(CloseReason.ProtocolError);
                                return false;
                            }
                            _header.HeaderReceiveState = Header.State.ReadingEncryptionKey;
                            break;

                        case Header.Type.Padding:
                            position++;
                            continue;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // Advance the position past the record type bit.
                    position++;
                }

                // Read the DH key
                if (_header.HeaderReceiveState == Header.State.ReadingCloseReason
                    && position < receiveLength)
                {
                    var reason =
                        (CloseReason)receiveBuffer[receiveOffset + position];

                    // Close the session.
                    Close(reason);
                    return false;
                }

                // Read the close reason.
                if (_header.HeaderReceiveState == Header.State.ReadingEncryptionKey
                    && position < receiveLength)
                {
                    var readLength = Math.Min(140 - (int)_negotiationStream.Length, receiveLength - position);
                    var encryptionKeyBuffer = new byte[readLength];
                    Buffer.BlockCopy(receiveBuffer, receiveOffset + position, encryptionKeyBuffer, 0,
                        readLength);

                    if (SecureConnectionReceive(encryptionKeyBuffer))
                        _header.Reset();

                    position += readLength;
                }

                // Read the number of bytes contained in the body.
                if (_header.HeaderReceiveState == Header.State.ReadingBodyLength
                    && position < receiveLength)
                {
                    // See if the buffer has any contents.
                    if (_header.BodyLengthBufferLength == 0)
                    {
                        if (position + 1 < count) // See if we can read the entire size at once.
                        {
                            _header.BodyLength = BitConverter.ToInt16(receiveBuffer,
                                receiveOffset + position);
                            position += 2;

                            // Body length complete.
                            _header.HeaderReceiveState = Header.State.Complete;
                        }
                        else
                        {
                            // Read the first byte of the body length.
                            _header.BodyLengthBuffer[0] =
                                receiveBuffer[receiveOffset + position];
                            _header.BodyLengthBufferLength = 1;
                            // Nothing more to read.
                            break;
                        }
                    }
                    else
                    {
                        // The buffer already contains a byte.
                        _header.BodyLengthBuffer[1] =
                            receiveBuffer[receiveOffset + position];
                        position++;

                        _header.BodyLength = BitConverter.ToInt16(_header.BodyLengthBuffer, 0);

                        // Body length complete.
                        _header.HeaderReceiveState = Header.State.Complete;
                    }
                }

                // If we do not have a complete receive header, stop parsing
                if (_header.HeaderReceiveState != Header.State.Complete)
                    break;

                // Reset the receive header buffer info.
                _header.BodyLengthBufferLength = 0;

                var currentMessageReadLength = Math.Min(_header.BodyLength - _header.BodyPosition,
                    receiveLength - position);

                if (currentMessageReadLength == 0)
                    break;

                var readBuffer = new byte[currentMessageReadLength];
                Buffer.BlockCopy(receiveBuffer, receiveOffset + position,
                    readBuffer, 0, currentMessageReadLength);

                _header.BodyPosition += currentMessageReadLength;
                position += currentMessageReadLength;

                HandleIncomingBytes(readBuffer);

                if (_header.BodyPosition == _header.BodyLength)
                {
                    _header.Reset();
                }
            }

            return true;
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
                    // Alert the other end of the connection that the session has been closed.
                    SendWithHeader(null, 0, 0, Header.Type.ConnectionClose, new[] {(byte) reason}, 0, 1);

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

            // Free the SocketAsyncEventArg so they can be reused by another client.
            _argsPool.Free(_sendArgs);
            _argsPool.Free(_receiveArgs);

            // Free the transformed buffer.
            _receiveTransformedBufferManager.FreeBuffer(_receiveTransformedBuffer);

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