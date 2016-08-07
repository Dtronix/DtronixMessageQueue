using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace DtronixMessageQueue {
	public class MqSession : AppSession<MqSession, RequestInfo<byte, byte[]>> {
		public MqMailbox Mailbox { get; set; }

		/// <summary>
		/// User supplied token used to pass a related object around with this session.
		/// </summary>
		public object Token { get; set; }


		public void Send(MqMessage message) {
			if (Connected == false) {
				throw new InvalidOperationException("Can not send messages while disconnected from server.");
			}

			Mailbox.EnqueueOutgoingMessage(message);
		}
	}
}