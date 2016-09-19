namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Interface used by RpcSession to execute events internally.
	/// </summary>
	public interface IProcessRpcSession {

		/// <summary>
		/// Called to cancel a remote waiting operation on the recipient connection.
		/// </summary>
		/// <param name="id">Id of the waiting operation to cancel.</param>
		void CancelWaitOperation(ushort id);

		/// <summary>
		/// Creates a waiting operation for this session.  Could be a remote cancellation request or a pending result request.
		/// </summary>
		/// <returns>Wait operation to wait on.</returns>
		RpcOperationWait CreateWaitOperation();
	}
}