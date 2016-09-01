namespace DtronixMessageQueue.Rpc {
	public class RpcClient<TSession> : MqClient<TSession>
		where TSession : RpcSession<TSession>, new() {

		public RpcClient(MqSocketConfig config) : base(config) {
		}



	}
}
