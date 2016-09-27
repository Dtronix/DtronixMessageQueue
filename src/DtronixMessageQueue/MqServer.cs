using System;
using System.Collections.Concurrent;
using System.Threading;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue {
	/// <summary>
	/// Message Queue Server to handle incoming clients
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public class MqServer<TSession, TConfig> : SocketServer<TSession, TConfig>
		where TSession : MqSession<TSession, TConfig>, new()
		where TConfig : MqConfig{

		/// <summary>
		/// Event fired when a new message arrives at a session's mailbox.
		/// </summary>
		public event EventHandler<IncomingMessageEventArgs<TSession, TConfig>> IncomingMessage;

		/// <summary>
		/// Initializes a new instance of a message queue.
		/// </summary>
		public MqServer(TConfig config) : base(config) {
			Setup();
		}

		/// <summary>
		/// Event method invoker
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The event object containing the mailbox to retrieve the message from.</param>
		private void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession, TConfig> e) {
			IncomingMessage?.Invoke(sender, e);
		}
		/// <summary>
		/// Called by the timer to verify that the session is still connected.  If it has timed out, close it.
		/// </summary>
		/// <param name="state">Concurrent dictionary of the sessions.</param>

		protected override TSession CreateSession(System.Net.Sockets.Socket session_socket) {
			var session = base.CreateSession(session_socket);
			session.IncomingMessage += OnIncomingMessage;

			return session;
		}

		protected override void OnClose(TSession session, SocketCloseReason reason) {
			TSession out_session;
			ConnectedSessions.TryRemove(session.Id, out out_session);

			session.IncomingMessage -= OnIncomingMessage;
			session.Dispose();
			base.OnClose(session, reason);
		}
	}
}