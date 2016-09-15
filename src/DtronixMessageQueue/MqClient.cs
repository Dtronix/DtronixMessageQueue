using System;
using System.Collections.Concurrent;
using System.Threading;
using DtronixMessageQueue.Socket;


namespace DtronixMessageQueue {

	/// <summary>
	/// Client used to connect to a remote message queue server.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public class MqClient<TSession, TConfig> : SocketClient<TSession, TConfig>
		where TSession : MqSession<TSession, TConfig>, new()
		where TConfig : MqConfig {

		/// <summary>
		/// Internal postmaster.
		/// </summary>
		private readonly MqPostmaster<TSession, TConfig> postmaster;

		/// <summary>
		/// Event fired when a new message arrives at the mailbox.
		/// </summary>
		public event EventHandler<IncomingMessageEventArgs<TSession, TConfig>> IncomingMessage;

		/// <summary>
		/// Timer used to verify that the sessions are still connected.
		/// </summary>
		private readonly Timer timeout_timer;

		/// <summary>
		/// Initializes a new instance of a message queue.
		/// </summary>
		public MqClient(TConfig config) : base(config) {

			// Override the default connection limit and read/write workers.
			config.MaxConnections = 1;
			config.MaxReadWriteWorkers = 4;
			timeout_timer = new Timer(TimeoutCallback);
			postmaster = new MqPostmaster<TSession, TConfig>(config);

			Setup();
		}

		protected override void OnConnect(TSession session) {
			// Start the timeout timer.
			var ping_frequency = ((MqConfig) Config).PingFrequency;

			if (ping_frequency > 0) {
				timeout_timer.Change(ping_frequency/2, ping_frequency);
			}

			base.OnConnect(session);
		}

		protected override void OnClose(TSession session, SocketCloseReason reason) {
			// Stop the timeout timer.
			timeout_timer.Change(Timeout.Infinite, Timeout.Infinite);

			base.OnClose(session, reason);
		}


		/// <summary>
		/// Called by the timer to verify that the session is still connected.  If it has timed out, close it.
		/// </summary>
		/// <param name="state">Concurrent dictionary of the sessions.</param>
		private void TimeoutCallback(object state) {
			Session.Send(Session.CreateFrame(null, MqFrameType.Ping));
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
			return session;
		}

		/// <summary>
		/// Adds a frame to the outbox to be processed.
		/// </summary>
		/// <param name="frame">Frame to send.</param>
		public void Send(MqFrame frame) {
			Send(new MqMessage(frame));
		}

		/// <summary>
		/// Adds a message to the outbox to be processed.
		/// Empty messages will be ignored.
		/// </summary>
		/// <param name="message">Message to send.</param>
		public void Send(MqMessage message) {
			if (message.Count == 0) {
				return;
			}

			// Enqueue the outgoing message to be processed by the postmaster.
			Session.EnqueueOutgoingMessage(message);
		}

		public void Close() {
			Session.IncomingMessage -= OnIncomingMessage;
			Session.Close(SocketCloseReason.ClientClosing);
			Session.Dispose();
		}

		/// <summary>
		/// Disposes of all resources associated with this client.
		/// </summary>
		public void Dispose() {
			postmaster.Dispose();
			timeout_timer.Dispose();

		}

	}
}