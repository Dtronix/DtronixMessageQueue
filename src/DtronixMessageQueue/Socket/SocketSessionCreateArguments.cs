using System;
using DtronixMessageQueue.Logging;

namespace DtronixMessageQueue.Socket
{

    /// <summary>
    /// Contains the arguments to create a TlsSocketSesstion
    /// </summary>
    /// <typeparam name="TSession"></typeparam>
    /// <typeparam name="TConfig"></typeparam>
    public class SocketSessionCreateArguments<TSession, TConfig>
        where TSession : SocketSession<TSession, TConfig>, new()
        where TConfig : SocketConfig
    {
        /// <summary>
        /// Socket this session is to use.
        /// </summary>
        public System.Net.Sockets.Socket SessionSocket;

        /// <summary>
        /// Socket configurations this session is to use.
        /// </summary>
        public TConfig SessionConfig;

        /// <summary>
        /// Handler base which is handling this session.
        /// </summary>
        public SocketHandler<TSession, TConfig> SocketHandler;

        /// <summary>
        /// Memory pool for receiving and sending of data.
        /// </summary>
        public BufferMemoryPool BufferMemoryPool;
    }
}