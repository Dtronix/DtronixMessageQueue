using System;
using DtronixMessageQueue.Rpc.DataContract;

namespace DtronixMessageQueue.Rpc
{
    /// <summary>
    /// Rpc class for containing client logic.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class RpcClient<TSession, TConfig> : MqClient<TSession, TConfig>
        where TSession : RpcSession<TSession, TConfig>, new()
        where TConfig : RpcConfig
    {
        /// <summary>
        /// Information about the connected server.
        /// </summary>
        public RpcServerInfoDataContract ServerInfo { get; set; }

        /// <summary>
        /// Called to send authentication data to the server.
        /// </summary>
        public event EventHandler<RpcAuthenticateEventArgs<TSession, TConfig>> Authenticate;

        /// <summary>
        /// Event invoked once when the RpcSession has been authenticated and is ready for usage.
        /// </summary>
        public event EventHandler<SessionEventArgs<TSession, TConfig>> Ready;


        public ServiceMethodCache ServiceMethodCache { get; set; }

        /// <summary>
        /// Initializes a new instance of a Rpc client.
        /// </summary>
        /// <param name="config">Configurations for this client to use.</param>
        public RpcClient(TConfig config) : base(config)
        {
            ServiceMethodCache = new ServiceMethodCache();
        }

        protected override void OnConnected(TSession session)
        {

            session.Ready += (sender, e) => { Ready?.Invoke(sender, e); };
            session.Authenticate += (sender, e) => { Authenticate?.Invoke(sender, e); };

            base.OnConnected(session);
        }
    }
}