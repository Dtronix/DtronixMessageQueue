using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc {
	public class RpcConfig : MqConfig {

		/// <summary>
		/// Number of threads used for executing RPC calls.
		/// </summary>
		public int MaxExecutionThreads { get; set; } = 10;
	}
}
