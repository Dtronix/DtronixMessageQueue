using System;

namespace DtronixMessageQueue.Socket {
	/// <summary>
	/// Event args used when a session is closed.
	/// </summary>
	/// <typeparam name="TSession">Session type.</typeparam>
	public class SessionClosedEventArgs<TSession> : EventArgs
		where TSession : SocketSession {

		/// <summary>
		/// Closed session.
		/// </summary>
		public TSession Session { get; }

		/// <summary>
		/// Reason the session was closed.
		/// </summary>
		public SocketCloseReason CloseReason { get; }

		/// <summary>
		/// Creates a new instance of the session closed event args.
		/// </summary>
		/// <param name="session">Closed session.</param>
		/// <param name="close_reason">Reason the session was closed.</param>
		public SessionClosedEventArgs(TSession session, SocketCloseReason close_reason) {
			Session = session;
			CloseReason = close_reason;
		}
	}
}
