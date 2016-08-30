using System;

namespace DtronixMessageQueue.Socket {
	public class SessionConnectedEventArgs<TSession> : EventArgs
	where TSession : SocketSession {

		public SessionConnectedEventArgs(TSession session) {
			Session = session;
		}

		public TSession Session { get; }
	}
}
