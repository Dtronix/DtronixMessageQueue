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
		/// Handles all incoming and outgoing messages
		/// </summary>
		private readonly MqPostmaster<TSession, TConfig> postmaster;

		/// <summary>
		/// Event fired when a new message arrives at a session's mailbox.
		/// </summary>
		public event EventHandler<IncomingMessageEventArgs<TSession, TConfig>> IncomingMessage;

		/// <summary>
		/// True if the timeout timer is running.  False otherwise.
		/// </summary>
		private bool timeout_timer_running = false;

		/// <summary>
		/// Timer used to verify that the sessions are still connected.
		/// </summary>
		private readonly Timer timeout_timer;

		/// <summary>
		/// Initializes a new instance of a message queue.
		/// </summary>
		public MqServer(TConfig config) : base(config) {
			timeout_timer = new Timer(TimeoutCallback);

			postmaster = new MqPostmaster<TSession, TConfig>(config);

			Setup();
		}

		/// <summary>
		/// Called by the timer to verify that the session is still connected.  If it has timed out, close it.
		/// </summary>
		/// <param name="state">Concurrent dictionary of the sessions.</param>
		private void TimeoutCallback(object state) {
			var timout_int = ((MqConfig) Config).PingTimeout;
			var timeout_time = DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 0, 0, timout_int));

			foreach (var session in ConnectedSessions.Values) {
				if (session.LastReceived < timeout_time) {
					session.Close(SocketCloseReason.TimeOut);
				}
			}

		}

		/// <summary>
		/// Event method invoker
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The event object containing the mailbox to retrieve the message from.</param>
		private void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession, TConfig> e) {
			IncomingMessage?.Invoke(sender, e);
		}

		protected override TSession CreateSession(System.Net.Sockets.Socket socket) {
			var session = base.CreateSession(socket);
			session.Postmaster = postmaster;
			session.IncomingMessage += OnIncomingMessage;
			session.BaseSocket = this;

			((ISetupSocketSession<TConfig>)session).Setup(socket, AsyncPool, Config);

			return session;
		}

		protected override void OnConnect(TSession session) {
			// Start the timeout timer if it is not already running.
			if (timeout_timer_running == false) {
				timeout_timer.Change(0, ((MqConfig)Config).PingTimeout);
				timeout_timer_running = true;
			}

			base.OnConnect(session);
		}


		protected override void OnClose(TSession session, SocketCloseReason reason) {
			TSession out_session;
			ConnectedSessions.TryRemove(session.Id, out out_session);

			// If there are no clients connected, stop the timer.
			if (ConnectedSessions.IsEmpty) {
				timeout_timer.Change(Timeout.Infinite, Timeout.Infinite);
				timeout_timer_running = false;
			}

			session.IncomingMessage -= OnIncomingMessage;
			session.Dispose();
			base.OnClose(session, reason);
		}
	}
}