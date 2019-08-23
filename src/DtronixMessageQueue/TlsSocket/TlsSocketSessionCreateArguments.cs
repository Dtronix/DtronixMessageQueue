using System;
using System.Net.Sockets;
using DtronixMessageQueue.TcpSocket;

//using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.TlsSocket
{

    /// <summary>
    /// Contains the arguments to create a TlsSocketSesstion
    /// </summary>
    /// <typeparam name="TSession"></typeparam>
    /// <typeparam name="TConfig"></typeparam>
    public class TlsSocketSessionCreateArguments
    {
        /// <summary>
        /// Socket this session is to use.
        /// </summary>
        public Socket SessionSocket;

        /// <summary>
        /// Socket configurations this session is to use.
        /// </summary>
        public TlsSocketConfig SessionConfig;

        /// <summary>
        /// Handler base which is handling this session.
        /// </summary>
        //public TcpSocketHandler<TSession, TConfig> TlsSocketHandler;

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

        public TcpSocketMode Mode;
    }
}