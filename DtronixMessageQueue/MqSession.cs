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

		public override void CloseConnection(SocketCloseReason reason) {
			var close_frame = new MqFrame(new byte[1]) {FrameType = MqFrameType.Command};
			close_frame.Write(0, (byte)reason);

			Mailbox.Stop(close_frame);

			base.CloseConnection(reason);
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


		/// <summary>
		/// Processes an incoming command frame from the connection.
		/// </summary>
		/// <param name="frame">Command frame to process.</param>
		internal void ProcessCommand(MqFrame frame) {
			var command_type = frame.ReadByte(0);

			switch (command_type) {
				case 0: // Closed
					CloseConnection((SocketCloseReason)frame.ReadByte(1));
					break;

			}
		}
	}
}