using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Socket {
	public class SessionConnectedEventArgs<TSession> : EventArgs
	where TSession : SocketSession {

		public SessionConnectedEventArgs(TSession session) {
			Session = session;
		}

		public TSession Session { get; }
	}
}
