namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Represents a remote service accessible through a RpcProxy object.
	/// </summary>
	/// <typeparam name="TSession">Session type.</typeparam>
	public interface IRemoteService<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Name of this service.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Session for this service instance.
		/// </summary>
		TSession Session { get; set; }
	}
}
