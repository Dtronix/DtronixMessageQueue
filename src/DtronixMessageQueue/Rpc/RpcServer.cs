using System;
using System.Net.Sockets;
using Amib.Threading;
using DtronixMessageQueue.Rpc.DataContract;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Rpc class for containing server logic.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public class RpcServer<TSession, TConfig> : MqServer<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Information about this server passed along to client's on connect.
		/// </summary>
		public RpcServerInfoDataContract ServerInfo { get; }

		/// <summary>
		/// Thread pool for all the Rpc call workers.
		/// </summary>
		public SmartThreadPool WorkerThreadPool { get; }

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
		public RpcServer(TConfig config) : this(config, new RpcServerInfoDataContract()) {
		}

		/// <summary>
		/// Creates a new instance of the server with the specified configurations.
		/// </summary>
		/// <param name="config">Configurations for this server.</param>
		/// <param name="server_info">Information to be passed to the client.</param>
		public RpcServer(TConfig config, RpcServerInfoDataContract server_info) : base(config) {
			WorkerThreadPool = new SmartThreadPool(config.ThreadPoolTimeout, config.MaxExecutionThreads, 1);
			ServerInfo = server_info ?? new RpcServerInfoDataContract();
		}

		protected override TSession CreateSession(System.Net.Sockets.Socket session_socket) {
			var session = base.CreateSession(session_socket);

			session.Ready += (sender, e) => { Ready?.Invoke(sender, e); };
			session.Authenticate += (sender, e) => { Authenticate?.Invoke(sender, e); };
			return session;
		}

		protected override void TimeoutCallback(object state) {
			var timout_int = Config.PingTimeout;
			var timeout_time = DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 0, 0, timout_int));

			foreach (var session in ConnectedSessions.Values) {
				if (session.LastReceived < timeout_time) {
					// Check for session timeout
					session.Close(SocketCloseReason.TimeOut);

				}else if (session.Authenticated == false && session.ConnectedTime < timeout_time) {
					// Ensure that failed authentications are removed.
					session.Close(SocketCloseReason.AuthenticationFailure);
				}
			}
		}
	}
}
