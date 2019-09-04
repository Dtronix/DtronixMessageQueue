using System;
using System.Net.Sockets;
//using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.TcpSocket
{

    /// <summary>
    /// Contains the arguments to create a TlsSocketSesstion
    /// </summary>
    /// <typeparam name="TSession"></typeparam>
    /// <typeparam name="TConfig"></typeparam>
    public class TcpSocketSessionCreateArguments<TSession, TConfig>
        where TSession : TcpSocketSession<TSession, TConfig>, new()
        where TConfig : TcpSocketConfig
    {
        /// <summary>
        /// Socket this session is to use.
        /// </summary>
        public Socket SessionSocket;

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
        //public ServiceMethodCache ServiceMethodCache;

        public BufferMemoryPool BufferPool;
    }
}