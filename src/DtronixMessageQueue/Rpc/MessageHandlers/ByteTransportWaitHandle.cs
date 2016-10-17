using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc.MessageHandlers {
	public class ByteTransportWaitHandle<TSession, TConfig> : ResponseWaitHandle
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		public ByteTransport<TSession, TConfig> ByteTransport { get; set; }
	}
}
