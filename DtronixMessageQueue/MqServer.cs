using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
		/// Initializes a new instance of a message queue.
		/// </summary>
		public MqServer(MqSocketConfig config) : base(config) {

			postmaster = new MqPostmaster {
				MaxReaders = config.MaxConnections + 1,
				MaxWriters = config.MaxConnections + 1
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
			session.Postmaster = postmaster;
			session.IncomingMessage += OnIncomingMessage;

			return session;
		}


		protected override void OnClose(MqSession session, SocketCloseReason reason) {
			session.IncomingMessage -= OnIncomingMessage;
			session.Dispose();
			base.OnClose(session, reason);
		}
	}
}