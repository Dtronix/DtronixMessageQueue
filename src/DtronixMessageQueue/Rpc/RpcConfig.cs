using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc {
	/// <summary>
	/// Configurations used by the Rpc sessions.
	/// </summary>
	public class RpcConfig : MqConfig {

		/// <summary>
		/// Number of threads used for executing RPC calls.
		/// </summary>
		public int MaxExecutionThreads { get; set; } = 5;

		/// <summary>
		/// Number of threads each session is allowed to use at a time from the main thread pool.
		/// </summary>
		public int MaxSessionConcurrency { get; set; } = 5;

		/// <summary>
		/// Set to true if the client needs to pass authentication data to the server to connect.
		/// </summary>
		public bool RequireAuthentication { get; set; } = false;

		/// <summary>
		/// Set to the maximum stream of bytes that is allowed to be sent to the session.
		/// </summary>
		public long MaxByteTransportLength { get; set; } = Int64.MaxValue;
	}
}
