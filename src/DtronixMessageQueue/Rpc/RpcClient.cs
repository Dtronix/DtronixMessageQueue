﻿using System;
using Amib.Threading;
using DtronixMessageQueue.Rpc.DataContract;

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
		/// VCalled to send authentication data to the server.
		/// </summary>
		public event EventHandler<RpcAuthenticateEventArgs> Authenticate;

		/// <summary>
		/// Initializes a new instance of a Rpc client.
		/// </summary>
		/// <param name="config">Configurations for this client to use.</param>
		public RpcClient(TConfig config) : base(config) {
			WorkerThreadPool = new SmartThreadPool(config.ThreadPoolTimeout, config.MaxExecutionThreads, 1);
		}



	}
}
