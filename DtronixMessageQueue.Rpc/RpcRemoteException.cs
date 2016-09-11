using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc {
	public class RpcRemoteException : Exception {
		public RpcRemoteException(RpcRemoteExceptionDataContract exception) : base(exception.Message) {
			
		}

	}
}
