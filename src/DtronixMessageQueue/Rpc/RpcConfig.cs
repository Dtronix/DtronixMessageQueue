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
		public int MaxExecutionThreads { get; set; } = 10;

		/// <summary>
		/// Set to true if the client needs to pass authentication data to the server to connect.
		/// </summary>
		public bool RequireAuthentication { get; set; } = false;
	}
}
