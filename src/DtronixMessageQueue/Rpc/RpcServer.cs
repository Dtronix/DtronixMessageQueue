using System;
using System.Net.Sockets;
using Amib.Threading;

namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Rpc class for containing server logic.
	/// </summary>
	/// <typeparam name="TSession">Session type for the client.</typeparam>
	public class RpcServer<TSession, TConfig> : MqServer<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Thread pool for all the Rpc call workers.
		/// </summary>
		public SmartThreadPool WorkerThreadPool { get; }

		/// <summary>
		/// Creates a new instance of the server with the specified configurations.
		/// </summary>
		/// <param name="config">Configurations for this server.</param>
		public RpcServer(TConfig config) : base(config) {
			WorkerThreadPool = new SmartThreadPool(config.IdleWorkerTimeout, config.MaxReadWriteWorkers, 1);
		}

		/// <summary>
		/// Override for the server.  Attaches the server to the session's Server property.
		/// </summary>
		/// <param name="socket">Socket which is attempting to connect.</param>
		/// <returns>New session.</returns>
		protected override TSession CreateSession(System.Net.Sockets.Socket socket) {
			var session = base.CreateSession(socket);
			session.Server = this;
			return session;
		}


	}
}
