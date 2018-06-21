using System;

namespace DtronixMessageQueue.Rpc
{
    /// <summary>
    /// Configurations used by the Rpc sessions.
    /// </summary>
    public class RpcConfig : MqConfig
    {
        /// <summary>
        /// Number of threads used for executing RPC calls.
        /// -1 sets number of threads to number of logical processors.
        /// </summary>
        public int MaxExecutionThreads { get; set; } = -1;

        /// <summary>
        /// Number of threads each session is allowed to use at a time from the main thread pool.
        /// </summary>
        public int MaxSessionConcurrency { get; } = 1;

        /// <summary>
        /// Set to true if the client needs to pass authentication data to the server to connect.
        /// </summary>
        public bool RequireAuthentication { get; set; } = false;

        /// <summary>
        /// Set to the maximum stream of bytes that is allowed to be sent to the session.
        /// </summary>
        public long MaxByteTransportLength { get; set; } = Int64.MaxValue;

        /// <summary>
        /// Number of seconds before a RPC call with a return value executes before timing out.
        /// </summary>
        public int RpcExecutionTimeout { get; set; } = 10000;
    }
}