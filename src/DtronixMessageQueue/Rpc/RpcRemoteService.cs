namespace DtronixMessageQueue.Rpc
{
    /// <summary>
    /// Represents a remote service accessible through a RpcProxy object.
    /// </summary>
    /// <typeparam name="TSession">Session type.</typeparam>
    /// <typeparam name="TConfig">Configuration type.</typeparam>
    public abstract class RpcRemoteService<TSession, TConfig>
        where TSession : RpcSession<TSession, TConfig>, new()
        where TConfig : RpcConfig
    {
        /// <summary>
        /// Name of this service.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Session for this service instance.
        /// </summary>
        public TSession Session { get; set; }
    }
}