using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ClientEngine;
using SuperSocket.ProtoBase;

namespace DtronixMessageQueue {
	public class MqClient : MqConnector {
		private readonly EasyClient<BufferedPackageInfo> connection;

		public MqClient() : base(1, 1) {
			connection = new EasyClient<BufferedPackageInfo>();
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

		public void Send(MqMessage message) {
			if (connection.Socket == null) {
				throw new InvalidOperationException("Can not send messages while disconnected from server.");
			}
			connection.Mailbox.EnqueueOutgoingMessage(message);
			
		}

		public void ProcessSend() {
			connection.Mailbox.ProcessOutbox();
		}
	}
}
