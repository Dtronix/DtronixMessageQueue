using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Socket;
using NLog;

namespace DtronixMessageQueue {
	public class MqSession : SocketSession {

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public MqMailbox Mailbox { get; set; }

		/// <summary>
		/// User supplied token used to pass a related object around with this session.
		/// </summary>
		public object Token { get; set; }

		protected override void HandleIncomingBytes(byte[] buffer) {
			Mailbox.EnqueueIncomingBuffer(buffer);
		}


		/// <summary>
		/// Sends a message to the session's client.
		/// </summary>
		/// <param name="message">Message to send.</param>
		public void Send(MqMessage message) {
			/*if (Connected == false) {
				throw new InvalidOperationException("Can not send messages while disconnected from server.");
			}*/

			if (message.Count == 0) {
				return;
			}

			Mailbox.EnqueueOutgoingMessage(message);
		}
	}
}