using System;

namespace DtronixMessageQueue.Socket {
	public class SessionClosedEventArgs<TSession> : EventArgs
		where TSession : SocketSession {

		public TSession Session { get; }
		public SocketCloseReason CloseReason { get; }

		public SessionClosedEventArgs(TSession session, SocketCloseReason close_reason) {
			Session = session;
			CloseReason = close_reason;
		}
	}
}
