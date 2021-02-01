using System;

namespace DtronixMessageQueue.Socket
{

    /// <summary>
    /// Contains the arguments to create a TlsSocketSesstion
    /// </summary>
    /// <typeparam name="TSession"></typeparam>
    /// <typeparam name="TConfig"></typeparam>
    public class TlsSocketSessionCreateArguments<TSession, TConfig>
        where TSession : TcpSocketSession<TSession, TConfig>, new()
        where TConfig : TcpSocketConfig
    {
        /// <summary>
        /// Socket this session is to use.
        /// </summary>
        public System.Net.Sockets.Socket SessionSocket;

        /// <summary>
        /// Argument pool for this session to use.  Pulls two asyncevents for reading and writing and returns them at the end of this socket's life.
        /// </summary>
        public SocketAsyncEventArgsManager SocketArgsManager;

        /// <summary>
        /// Socket configurations this session is to use.
        /// </summary>
        public TConfig SessionConfig;

        /// <summary>
        /// Handler base which is handling this session.
        /// </summary>
        public TcpSocketHandler<TSession, TConfig> TlsSocketHandler;

        /// <summary>
        /// Processor which handles all inbox data.
        /// </summary>
        public ActionProcessor<Guid> InboxProcessor;

        /// <summary>
        /// Processor which handles all outbox data.
        /// </summary>
        public ActionProcessor<Guid> OutboxProcessor;

        /// <summary>
        /// Cache for commonly called methods used throughout the session.
        /// </summary>
        public ServiceMethodCache ServiceMethodCache;

        /// <summary>
        /// Receive buffer manager used to encryption trasformations on received data.
        /// </summary>
        public BufferManager ReceiveBufferManager;
    }
}