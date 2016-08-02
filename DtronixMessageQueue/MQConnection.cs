using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue {

	public class MQConnection {
		public readonly MQConnector Connector;
		public readonly MQMailbox Mailbox;
		public readonly Guid Id;
		public Socket Socket;
		public readonly MQFrameBuilder FrameBuilder;
		public readonly Semaphore WriterSemaphore = new Semaphore(1, 1);
		public SocketAsyncEventArgs SocketAsyncEvent;

		private MQConnection(MQConnector connector) {
			Id = Guid.NewGuid();
			Connector = connector;
			FrameBuilder = new MQFrameBuilder(Connector.ClientBufferSize);
			Mailbox = new MQMailbox(this);
		}

		public static MQConnection Create
	}
}
