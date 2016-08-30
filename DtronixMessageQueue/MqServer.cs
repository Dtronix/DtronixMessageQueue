using System;
using System.Collections.Concurrent;
using System.Threading;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue {
	/// <summary>
	/// Message Queue Server to handle incoming clients
	/// </summary>
	public class MqServer : SocketServer<MqSession> {
		private readonly MqPostmaster postmaster;

		/// <summary>
		/// Event fired when a new message arrives at a session's mailbox.
		/// </summary>
		public event EventHandler<IncomingMessageEventArgs> IncomingMessage;

		/// <summary>
		/// Timer used to verify that the sessions are still connected.
		/// </summary>
		private Timer timeout_timer;

		/// <summary>
		/// Initializes a new instance of a message queue.
		/// </summary>
		public MqServer(MqSocketConfig config) : base(config) {
			timeout_timer = new Timer(TimeoutCallback, ConnectedSessions, 0, config.PingTimeout);

			postmaster = new MqPostmaster(config);

			Setup();


		}

		/// <summary>
		/// Called by the timer to verify that the session is still connected.  If it has timed out, close it.
		/// </summary>
		/// <param name="state">Concurrent dictionary of the sessions.</param>
		private void TimeoutCallback(object state) {
			var sessions = state as ConcurrentDictionary<Guid, MqSession>;
			if (sessions.IsEmpty) {
				return;
			}

			var timout_int = ((MqSocketConfig) Config).PingTimeout;
			var timeout_time = DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 0, 0, timout_int));

			foreach (var session in sessions.Values) {
				if (session.LastReceived < timeout_time) {
					session.CloseConnection(SocketCloseReason.TimeOut);
				}
			}

		}

		/// <summary>
		/// Event method invoker
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The event object containing the mailbox to retrieve the message from.</param>
		private void OnIncomingMessage(object sender, IncomingMessageEventArgs e) {
			IncomingMessage?.Invoke(sender, e);
		}

		protected override MqSession CreateSession(System.Net.Sockets.Socket socket) {
			var session = base.CreateSession(socket);
			session.Postmaster = postmaster;
			session.IncomingMessage += OnIncomingMessage;
			session.BaseSocket = this;
			return session;
		}


		protected override void OnClose(MqSession session, SocketCloseReason reason) {
			MqSession out_session;
			ConnectedSessions.TryRemove(session.Id, out out_session);

			session.IncomingMessage -= OnIncomingMessage;
			session.Dispose();
			base.OnClose(session, reason);
		}
	}
}