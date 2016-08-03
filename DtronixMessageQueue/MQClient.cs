using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace DtronixMessageQueue {
	public class MQClient : MQConnector {
		private readonly MQConnection connection;

		public MQClient() : base(1, 1) {
			connection = CreateConnection();
		}

		public void Connect(string address, int port = 2828) {
			Connect(new IPEndPoint(IPAddress.Parse(address), port));

			// Once the client is connected, store the information.
			Connected += (sender, args) => {
				connection.Socket = args.ConnectSocket;
				connection.Socket.SendBufferSize = 0;
				connection.Socket.NoDelay = true;
				connection.SocketAsyncEvent = args;
			};

		}

		public void Connect(IPEndPoint end_point) {
			MainSocket = new Socket(end_point.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {
				NoDelay = true
			};

			var read_ea = ReadPool.Pop();
			read_ea.RemoteEndPoint = end_point;

			MainSocket.ConnectAsync(read_ea);
		}

		public void Send(MQMessage message) {
			if (connection.Socket == null) {
				throw new InvalidOperationException("Can not send messages while disconnected from server.");
			}
			connection.Mailbox.EnqueueOutgoingMessage(message);
			
		}
	}
}
