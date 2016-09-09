﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using DtronixMessageQueue.Socket;
using NLog;

namespace DtronixMessageQueue {
	/// <summary>
	/// Message Queue Server to handle incoming clients
	/// </summary>
	public class MqServer<TSession> : SocketServer<TSession>
		where TSession : MqSession<TSession>, new() {

		/// <summary>
		/// Logger for this class.
		/// </summary>
		private static ILogger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Handles all incoming and outgoing messages
		/// </summary>
		private readonly MqPostmaster<TSession> postmaster;

		/// <summary>
		/// Event fired when a new message arrives at a session's mailbox.
		/// </summary>
		public event EventHandler<IncomingMessageEventArgs<TSession>> IncomingMessage;

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
		public MqServer(MqSocketConfig config) : base(config) {
			timeout_timer = new Timer(TimeoutCallback);

			postmaster = new MqPostmaster<TSession>(config);

			Setup();
		}

		/// <summary>
		/// Called by the timer to verify that the session is still connected.  If it has timed out, close it.
		/// </summary>
		/// <param name="state">Concurrent dictionary of the sessions.</param>
		private void TimeoutCallback(object state) {
			var timout_int = ((MqSocketConfig) Config).PingTimeout;
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
		private void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession> e) {
			IncomingMessage?.Invoke(sender, e);
		}

		protected override TSession CreateSession(System.Net.Sockets.Socket socket) {
			var session = base.CreateSession(socket);
			session.Postmaster = postmaster;
			session.IncomingMessage += OnIncomingMessage;
			session.BaseSocket = this;

			return session;
		}

		protected override void OnConnect(TSession session) {
			// Start the timeout timer if it is not already running.
			if (timeout_timer_running == false) {
				timeout_timer.Change(0, ((MqSocketConfig)Config).PingTimeout);
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