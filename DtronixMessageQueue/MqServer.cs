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
	public class MqServer : SocketServer<MqSession> {
		private readonly MqPostmaster postmaster;

		/// <summary>
		/// Event fired when a new message arrives at a session's mailbox.
		/// </summary>
		public event EventHandler<IncomingMessageEventArgs> IncomingMessage;

		/// <summary>
		/// Initializes a new instance of a message queue.
		/// </summary>
		public MqServer(SocketConfig config) : base(config) {

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





		/*protected override void ExecuteCommand(MqSession session, RequestInfo<byte, byte[]> request_info) {
			try {
				if (request_info.Header == 0) {
					session.Mailbox.EnqueueIncomingBuffer(request_info.Body);
					//} else if (request_info.Header == 1) {
					// TODO: Setup configurations that the client must send before successfully connecting.
					// request_info.
				} else {
					// If the first byte is anything else, the data is either corrupted or another protocol.
					session.Close(CloseReason.ApplicationError);
				}
			} catch (Exception) {
				session.Close(CloseReason.ApplicationError);
				return;
			}
			base.ExecuteCommand(session, request_info);
		}*/
	}
}