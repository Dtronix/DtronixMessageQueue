using System;

namespace DtronixMessageQueue.Socket {

	/// <summary>
	/// Event args used when the session has connected to a remote endpoint.
	/// </summary>
	/// <typeparam name="TSession">Session type.</typeparam>
	public class SessionConnectedEventArgs<TSession> : EventArgs
		where TSession : SocketSession {

		/// <summary>
		/// Connected session.
		/// </summary>
		public TSession Session { get; }

		/// <summary>
		/// Creates a new instance of the session connected event args.
		/// </summary>
		/// <param name="session">Connected session.</param>
		public SessionConnectedEventArgs(TSession session) {
			Session = session;
		}

	}
}
