using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO;
using NLog;

namespace DtronixMessageQueue {
	public class MQServer : IDisposable {

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public class Client {
			public MQFrame Frame;
			public MQMessage Message;
			public MQMailbox Mailbox;
			public Guid Id;
			public AsyncSocket Socket;
			public byte[] Bytes = new byte[1024 * 16];
			public object WriteLock = new object();

		}

		public event EventHandler<IncomingMessageEventArgs> OnIncomingMessage;


		public class Config {
			public int MinimumWorkers { get; set; } = 4;

			/// <summary>
			/// Maximum backlog for pending connections.
			/// The default value is 100.
			/// </summary>
			public int ListenerBacklog { get; set; } = 100;
		}

		private readonly Config configurations;

		private readonly CompletionPort listen_completion_port;
		private readonly CompletionPort worker_completion_port;
		private readonly AsyncSocket listener;
		private readonly MQIOWorker listen_worker;


		private bool is_running;

		private readonly Dictionary<Guid, Client> connected_clients = new Dictionary<Guid, Client>();
		private readonly List<MQIOWorker> workers = new List<MQIOWorker>();


		public MQServer(Config configurations) {
			this.configurations = configurations;
			listen_completion_port = CompletionPort.Create();
			worker_completion_port = CompletionPort.Create();

			listener = AsyncSocket.Create(AddressFamily.InterNetwork,
				SocketType.Stream, ProtocolType.Tcp);

			listen_completion_port.AssociateSocket(listener, listener);

			listen_worker = new MQIOWorker(listen_completion_port, "mq-server-listener");

			for (var i = 0; i < this.configurations.MinimumWorkers; i++) {
				var worker = new MQIOWorker(worker_completion_port, "mq-server-worker");
				worker.OnReceive += Worker_OnReceive;
				workers.Add(worker);
				
			}

			listen_worker.OnAccept += Listen_worker_OnAccept;

			

			listener.Bind(new IPEndPoint(IPAddress.Any, 2828));

			

		}

		private void Worker_OnReceive(object sender, MQIOWorker.WorkerEventArgs e) {
			var client = e.Status.State as Client;
			if (client != null) {
				if (client.Message == null) {
					client.Message = new MQMessage();
				}
				int over_read = e.Status.BytesTransferred;
				while ((over_read = client.Message.Write(client.Bytes, e.Status.BytesTransferred - over_read, over_read)) > 0) {
					if (client.Message.Complete == false) {
						continue;
					}
					// We have a complete message.  Send it to the mailbox.
					client.Mailbox.Enqueue(client.Message);
					client.Message = new MQMessage();
				}

				if (client.Message.Complete) {
					client.Mailbox.Enqueue(client.Message);
					OnIncomingMessage?.Invoke(this, new IncomingMessageEventArgs(client.Mailbox, client.Id));
				}

				e.Status.AsyncSocket.Receive(client.Bytes, 0, client.Bytes.Length, SocketFlags.None);
			}
		}

		private void Listen_worker_OnAccept(object sender, MQIOWorker.WorkerEventArgs e) {
			var socket = e.Status.AsyncSocket.GetAcceptedSocket();
			var guid = new Guid();
			var client_cl = new Client {
				Id = guid,
				Socket = socket,
				Mailbox = new MQMailbox()
				
			};

			connected_clients.Add(guid, client_cl);

			// Add the new socket to the worker completion port.
			worker_completion_port.AssociateSocket(socket, client_cl);

			// Signal a worker that the new client has connected and to handle the work.
			worker_completion_port.Signal(client_cl);

			// Set the first receive.
			socket.Receive(client_cl.Bytes, 0, client_cl.Bytes.Length, SocketFlags.None);
		}


		public void Start() {
			if (is_running) {
				throw new InvalidOperationException("Server is already running.");
			}

			// Start all the workers.
			for (var i = 0; i < configurations.MinimumWorkers; i++) {
				workers[i].Start();
			}

			listener.Listen(configurations.ListenerBacklog);
			listen_worker.Start();

			listener.Accept();
		}

		public void Stop() {
			if (is_running == false) {
				throw new InvalidOperationException("Server is not running.");
			}

			// Stop all the workers.
			for (var i = 0; i < configurations.MinimumWorkers; i++) {
				workers[i].Stop();
			}

			listen_completion_port.Signal(null);

			is_running = false;
		}


		private void Listen() {
			is_running = true;
			var cancel = false;

			while (!cancel) {
				CompletionStatus completion_status;

				if (listen_completion_port.GetQueuedCompletionStatus(5000, out completion_status) == false) {
					continue;
				}

				switch (completion_status.OperationType) {
					case OperationType.Accept:
						

						break;

					case OperationType.Connect:
						break;

					case OperationType.Disconnect:
						break;

					case OperationType.Signal:
						var state = completion_status.State as Client;
						if (state != null) {
							// If the state is another socket, this is a disconnected client that needs to be removed from the list.
							var client = state;

							if (connected_clients.Remove(client.Id) == false) {
								logger.Error("Client {0} was not able to be removed from the list of active clients", client.Id);
							}

						} else if (completion_status.State == null) {
							cancel = true;
							is_running = false;
						}

						break;
				}
			}
		}
		

		public void Dispose() {
			if (is_running) {
				Stop();
			}

			listen_completion_port.Dispose();
			worker_completion_port.Dispose();


			foreach (var worker in workers) {
				worker.Dispose();
			}
		}
	}

}
