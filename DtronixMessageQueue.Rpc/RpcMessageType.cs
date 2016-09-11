namespace DtronixMessageQueue.Rpc {
	public enum RpcMessageType : byte {
		Unset = 0,
		Command = 1,
		RpcCall = 2,
		RpcCallNoReturn = 3,
		RpcCallReturn = 4,
		RpcCallException = 5
	}
}
