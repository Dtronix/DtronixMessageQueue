namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Rpc class for containing client logic.
	/// </summary>
	/// <typeparam name="TSession">Session type for the client.</typeparam>
	public class RpcClient<TSession> : MqClient<TSession>
		where TSession : RpcSession<TSession>, new() {

		/// <summary>
		/// Initializes a new instance of a Rpc client.
		/// </summary>
		/// <param name="config">Configurations for this client to use.</param>
		public RpcClient(MqSocketConfig config) : base(config) {
		}



	}
}
