using System;
using Amib.Threading;
using DtronixMessageQueue.Rpc.DataContract;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Rpc class for containing client logic.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public class RpcClient<TSession, TConfig> : MqClient<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Thread pool for all the Rpc call workers.
		/// </summary>
		public SmartThreadPool WorkerThreadPool { get; }

		/// <summary>
		/// Information about the connected server.
		/// </summary>
		public RpcServerInfoDataContract ServerInfo { get; set; }

		/// <summary>
		/// Called to send authentication data to the server.
		/// </summary>
		public event EventHandler<RpcAuthenticateEventArgs<TSession,TConfig>> Authenticate;

		/// <summary>
		/// Called when the authentication process concludes.
		/// </summary>
		public event EventHandler<RpcAuthenticateEventArgs<TSession, TConfig>> AuthenticationResult;

		/// <summary>
		/// Event invoked once when the RpcSession has been authenticated and is ready for usage.
		/// </summary>
		public event EventHandler<SessionEventArgs<TSession, TConfig>> Ready;

		/// <summary>
		/// Initializes a new instance of a Rpc client.
		/// </summary>
		/// <param name="config">Configurations for this client to use.</param>
		public RpcClient(TConfig config) : base(config) {
			WorkerThreadPool = new SmartThreadPool(config.ThreadPoolTimeout, config.MaxExecutionThreads, 1);
		}

		protected override TSession CreateSession() {
			var session = base.CreateSession();

			session.Ready += (sender, e) => { Ready?.Invoke(sender, e); };
			session.Authenticate += (sender, e) => { Authenticate?.Invoke(sender, e); };
			session.AuthenticationResult += (sender, e) => { AuthenticationResult?.Invoke(sender, e); };
			return session;
		}
	}
}
