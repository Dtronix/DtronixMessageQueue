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
		private Connection connection;

		public MQClient() : base(1, 1) {
			connection = new Connection {
				Mailbox = new MQMailbox()
			};
		}

		public void Connect(string address, int port = 2828) {
			Connect(new IPEndPoint(IPAddress.Parse(address), port));

			// Once the client is connected, store the information.
			Connected += (sender, args) => {
				connection.Socket = args.ConnectSocket;
				connection.SocketAsyncEvent = args;
			};

		}

		public void Connect(IPEndPoint end_point) {
			MainSocket = new Socket(end_point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			var read_ea = ReadPool.Pop();
			read_ea.RemoteEndPoint = end_point;

			MainSocket.ConnectAsync(read_ea);
		}


		public void Send(MQMessage message) {
			foreach (var frame in message.Frames) {
				var bytes = frame.RawFrame();
				Send(connection, bytes, 0, bytes.Length);
			}
			//client_socket.Send(bytes, 0, bytes.Length, SocketFlags.None);
		}

		public void Dispose() {

		}
	}
}
