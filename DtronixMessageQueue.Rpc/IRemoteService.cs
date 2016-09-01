namespace DtronixMessageQueue.Rpc {
	public interface IRemoteService<TSession>
		where TSession : RpcSession<TSession>, new() {
		string Name { get; }
		TSession Session { get; set; }
	}
}
