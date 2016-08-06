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
	}
}
