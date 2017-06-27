using System;
using DtronixMessageQueue.Rpc.DataContract;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Rpc
{
    /// <summary>
    /// Rpc class for containing server logic.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class RpcServer<TSession, TConfig> : MqServer<TSession, TConfig>
        where TSession : RpcSession<TSession, TConfig>, new()
        where TConfig : RpcConfig
    {
        /// <summary>
        /// Information about this server passed along to client's on connect.
        /// </summary>
        public RpcServerInfoDataContract ServerInfo { get; }

        /// <summary>
        /// Called to send authentication data to the server.
        /// </summary>
        public event EventHandler<RpcAuthenticateEventArgs<TSession, TConfig>> Authenticate;

        /// <summary>
        /// Event invoked once when the RpcSession has been authenticated and is ready for usage.
        /// </summary>
        public event EventHandler<SessionEventArgs<TSession, TConfig>> Ready;

        /// <summary>
        /// Creates a new instance of the server with the specified configurations.
        /// </summary>
        /// <param name="config">Configurations for this server.</param>
        public RpcServer(TConfig config) : this(config, new RpcServerInfoDataContract())
        {
        }

        /// <summary>
        /// Creates a new instance of the server with the specified configurations.
        /// </summary>
        /// <param name="config">Configurations for this server.</param>
        /// <param name="serverInfo">Information to be passed to the client.</param>
        public RpcServer(TConfig config, RpcServerInfoDataContract serverInfo) : base(config)
        {
            ServerInfo = serverInfo ?? new RpcServerInfoDataContract();
        }

        protected override TSession CreateSession(System.Net.Sockets.Socket sessionSocket)
        {
            var session = base.CreateSession(sessionSocket);

            session.Ready += (sender, e) => { Ready?.Invoke(sender, e); };
            session.Authenticate += (sender, e) => { Authenticate?.Invoke(sender, e); };
            return session;
        }

        protected override void TimeoutCallback(object state)
        {
            var timoutInt = Config.PingTimeout;
            var timeoutTime = DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 0, 0, timoutInt));

            foreach (var session in ConnectedSessions.Values)
            {
                if (session.LastReceived < timeoutTime)
                {
                    // Check for session timeout
                    session.Close(SocketCloseReason.TimeOut);
                }
                else if (session.Authenticated == false && session.ConnectedTime < timeoutTime)
                {
                    // Ensure that failed authentications are removed.
                    session.Close(SocketCloseReason.AuthenticationFailure);
                }
            }
        }
    }
}