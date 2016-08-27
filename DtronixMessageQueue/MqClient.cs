using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Socket;


namespace DtronixMessageQueue {

	/// <summary>
	/// Client used to connect to a remote message queue server.
	/// </summary>
	public class MqClient : SocketClient<MqSession> {

		/// <summary>
		/// Internal postmaster.
		/// </summary>
		private readonly MqPostmaster postmaster;

		/// <summary>
		/// Event fired when a new message arrives at the mailbox.
		/// </summary>
		public event EventHandler<IncomingMessageEventArgs> IncomingMessage;


		/// <summary>
		/// Initializes a new instance of a message queue.
		/// </summary>
		public MqClient(SocketConfig config) : base(config) {
			config.MaxConnections = 1;

			postmaster = new MqPostmaster {
				MaxReaders = 2,
				MaxWriters = 2
			};

			Setup();
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
			session.Mailbox = new MqMailbox(postmaster, session);
			session.Mailbox.IncomingMessage += OnIncomingMessage;

			return session;
		}

		/// <summary>
		/// Adds a message to the outbox to be processed.
		/// Empty messages will be ignored.
		/// </summary>
		/// <param name="message">Message to send.</param>
		public void Send(MqMessage message) {
			/*if (IsConnected == false) {
				throw new InvalidOperationException("Can not send messages while disconnected from server.");
			}*/

			if (message.Count == 0) {
				return;
			}

			// Enqueue the outgoing message to be processed by the postmaster.
			Session.Mailbox.EnqueueOutgoingMessage(message);
		}


		public void Close() {
			Session.Mailbox.IncomingMessage -= OnIncomingMessage;
			Session.CloseConnection(SocketCloseReason.ClientClosing);
			Session.Mailbox.Dispose();
		}

		/// <summary>
		/// Disposes of all resources associated with this client.
		/// </summary>
		public void Dispose() {
			postmaster.Dispose();
			
		}

	}
}