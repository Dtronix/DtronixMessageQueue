using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Socket {
	public class SocketConfig {
		public int MaxConnections { get; set; } = 1;

		/// <summary>
		/// Maximum backlog for pending connections.
		/// The default value is 100.
		/// </summary>
		public int ListenerBacklog { get; set; } = 100;

		public int SendAndReceiveBufferSize { get; set; } = 1024*4;

		public int SendTimeout { get; set; } = 5000;
	}
}
