namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Rpc class for containing client logic.
	/// </summary>
	/// <typeparam name="TSession">Session type for the client.</typeparam>
	public class RpcClient<TSession, TConfig> : MqClient<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Initializes a new instance of a Rpc client.
		/// </summary>
		/// <param name="config">Configurations for this client to use.</param>
		public RpcClient(TConfig config) : base(config) {
		}



	}
}
