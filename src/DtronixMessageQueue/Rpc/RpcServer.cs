using System;
using System.Net.Sockets;
using Amib.Threading;

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
		/// Thread pool for all the Rpc call workers.
		/// </summary>
		public SmartThreadPool WorkerThreadPool { get; }

		/// <summary>
		/// Creates a new instance of the server with the specified configurations.
		/// </summary>
		/// <param name="config">Configurations for this server.</param>
		public RpcServer(TConfig config) : base(config) {
			WorkerThreadPool = new SmartThreadPool(config.ThreadPoolTimeout, config.MaxExecutionThreads, 1);
		}

	}
}
