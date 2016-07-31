using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AsyncIO;
using NLog;

namespace DtronixMessageQueue {
	public class MQClient : IDisposable {

		private readonly List<MQIOWorker> workers = new List<MQIOWorker>();

		private CompletionPort worker_completion_port;
		private AsyncSocket client_socket;



		public MQClient() {

			worker_completion_port = CompletionPort.Create();

			client_socket = AsyncSocket.Create(AddressFamily.InterNetwork,
				SocketType.Stream, ProtocolType.Tcp);

			worker_completion_port.AssociateSocket(client_socket);

			for (var i = 0; i < 4; i++) {
				var worker = new MQIOWorker(worker_completion_port);
				worker.OnConnect += Worker_OnConnect;
				workers.Add(worker);
			}
		}

		public void Connect(string address, int port = 2828) {
			foreach (var worker in workers) {
				worker.Start();
			}

			client_socket.Connect(IPAddress.Parse(address), port);
		}

		private void Worker_OnConnect(object sender, MQIOWorker.WorkerEventArgs e) {
			/*var bytes = new List<byte>();
			bytes.Add(1);
			bytes.AddRange(BitConverter.GetBytes(1));
			bytes.Add(236);
			e.Status.AsyncSocket.Send(bytes.ToArray(), 0, bytes.Count, SocketFlags.None);*/
		}

		public void Send(MQMessage message) {

			var bytes = message.ToByteArray();

			client_socket.Send(bytes, 0, bytes.Length, SocketFlags.None);
		}

		public void Dispose() {
			foreach (var worker in workers) {
				worker.Stop();
			}

			client_socket.Dispose();
			worker_completion_port.Dispose();
		}
	}
}
