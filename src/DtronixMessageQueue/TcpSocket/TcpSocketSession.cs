using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using DtronixMessageQueue.TlsSocket;
using DtronixMessageQueue.Transports;

//using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.TcpSocket
{
    /// <summary>
    /// Base socket session to be sub-classes by the implementer.
    /// </summary>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    /// <typeparam name="TSession">Session for this connection.</typeparam>
    public abstract class TcpSocketSession<TSession, TConfig> : ISetupSocketSession
        where TSession : TcpSocketSession<TSession, TConfig>, new()
        where TConfig : TcpSocketConfig
    {
        /// <summary>
        /// Current state of the socket.
        /// </summary>
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
        /// Processor to handle all inbound messages.
        /// </summary>
        protected ActionProcessor<Guid> InboxProcessor;

        /// <summary>
        /// Processor to handle all outbound messages.
        /// </summary>
        protected ActionProcessor<Guid> OutboxProcessor;

        private ITransportSession _transportSession;




        /// <summary>
        /// Cache for commonly called methods used throughout the session.
        /// </summary>
        //public ServiceMethodCache ServiceMethodCache;

        /// <summary>
        /// This event fires when a connection has been established.
        /// </summary>
        public event EventHandler<SessionEventArgs<TSession, TConfig>> Connected;

        /// <summary>
        /// This event fires when a connection has been shutdown.
        /// </summary>
        public event EventHandler<SessionClosedEventArgs<TSession, TConfig>> Closed;

        /// <summary>
        /// Creates a new socket session with a new Id.
        /// </summary>
        protected TcpSocketSession()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Sets up this socket with the specified configurations.
        /// </summary>
        /// <param name="args">Args to initialize the socket with.</param>
        public static TSession Create(TcpSocketSessionCreateArguments<TSession, TConfig> args)
        {
            var session = new TSession
            {
                _config = args.SessionConfig,
                SocketHandler = args.TlsSocketHandler,
                InboxProcessor = args.InboxProcessor,
                OutboxProcessor = args.OutboxProcessor,
                //ServiceMethodCache = args.ServiceMethodCache,
            };

            session.OnSetup();

            return session;
        }

        /// <summary>
        /// Start the session's receive events.
        /// </summary>
        void ISetupSocketSession.StartSession()
        {
            ConnectedTime = DateTime.UtcNow;

            _transportSession.Start();
            // TODO -----------------------

            // Send the protocol version number along with the public key to the connected client.
            if (SocketHandler.Mode == TransportMode.Client)
            {
                // Authenticate client
            }
        }

        private bool SecureConnectionReceive(byte[] buffer)
        {
            if (SocketHandler.Mode == TransportMode.Server)
            {
                _config.Logger?.Trace($"{SocketHandler.Mode}: Sending client public ECDH key.");
            }


            SecureConnectionComplete();
            return true;
        }


        private void SecureConnectionComplete()
        {
            _config.Logger?.Trace($"{SocketHandler.Mode}: Securing complete.");
        }

        /// <summary>
        /// Called after the initial setup has occurred on the session.
        /// </summary>
        protected abstract void OnSetup();

        /// <summary>
        /// Sends raw bytes to the socket.  Blocks until data is sent to the underlying system to send.
        /// Before transport encryption has been established, any buffer size will be sent.
        /// After transport encryption has been established, only buffers in increments of 16.
        /// Excess will be buffered until the next write.
        /// </summary>
        /// <param name="buffer">Buffer to copy and send.</param>
        protected virtual void Send(ReadOnlyMemory<byte> buffer)
        {
            _transportSession.Send(buffer);
        }




        /// <summary>
        /// Called when this session is desired or requested to be closed.
        /// </summary>
        /// <param name="reason">Reason this socket is closing.</param>
        public virtual void Close(CloseReason reason)
        {


            _config.Logger?.Trace($"{SocketHandler.Mode}: Connection closed. Reason: {reason}.");


            InboxProcessor.Deregister(Id);
            OutboxProcessor.Deregister(Id);

            _transportSession.Close();

        }

        /// <summary>
        /// String representation of the active session.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            return $"{SocketHandler.Mode} TcpSocketSession;";
        }
    }
}