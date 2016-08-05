using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace DtronixMessageQueue {


	public class MqSession : AppSession<MqSession, BinaryRequestInfo> {
		public readonly MQConnector Connector;
		public readonly MQMailbox Mailbox;
		public readonly MQFrameBuilder FrameBuilder;

		public MqSession() {
			
		}
	}
}
