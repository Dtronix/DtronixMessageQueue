using System;
using System.Collections.Concurrent;
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

		public const int ClientBufferSize = 1024 * 16;

		public class Client {
			public MQFrame Frame;
			public MQMessage Message;
			public MQMailbox Mailbox;
			public Guid Id;
			public AsyncSocket Socket;
			public byte[] Bytes = new byte[ClientBufferSize];
			public MQFrameBuilder FrameBuilder = new MQFrameBuilder(ClientBufferSize);
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

		private readonly ConcurrentDictionary<Guid, Client> connected_clients = new ConcurrentDictionary<Guid, Client>();
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
				worker.OnDisconnect += Worker_OnDisconnect;
				workers.Add(worker);
				
			}

			listen_worker.OnAccept += Listen_worker_OnAccept;

			

			listener.Bind(new IPEndPoint(IPAddress.Any, 2828));

			

		}

		private void Worker_OnDisconnect(object sender, MQIOWorker.WorkerDisconnectEventArgs e) {
			var client = e.Status.State as Client;
			if (client == null) {
				return;
			}

			client.FrameBuilder.Dispose();

			Client cli;
			if (connected_clients.TryRemove(client.Id, out cli) == false) {
				logger.Fatal("Client {0} was not able to be removed from the list of clients.", client.Id);
			}
			


		}

		private void Worker_OnReceive(object sender, MQIOWorker.WorkerEventArgs e) {
			var client = e.Status.State as Client;
			if (client == null) {
				return;
			}

			if (client.Message == null) {
				client.Message = new MQMessage();
			}
			try {
				client.FrameBuilder.Write(client.Bytes, 0, e.Status.BytesTransferred);
			} catch (InvalidDataException ex) {
				client.Socket.Dispose();
				return;
			}

			var frame_count = client.FrameBuilder.Frames.Count;

			for (var i = 0; i < frame_count; i++) {
				var frame = client.FrameBuilder.Frames.Dequeue();
				client.Message.Add(frame);

				if (frame.FrameType == MQFrameType.EmptyLast || frame.FrameType == MQFrameType.Last) {
					client.Mailbox.Enqueue(client.Message);
					client.Message = new MQMessage();
					OnIncomingMessage?.Invoke(this, new IncomingMessageEventArgs(client.Mailbox, client.Id));
				}
			}
		}

		private void Listen_worker_OnAccept(object sender, MQIOWorker.WorkerEventArgs e) {
			var socket = e.Status.AsyncSocket.GetAcceptedSocket();
			var guid = Guid.NewGuid();
			var client_cl = new Client {
				Id = guid,
				Socket = socket,
				Mailbox = new MQMailbox()
				
			};

			if (connected_clients.TryAdd(guid, client_cl) == false) {
				logger.Fatal("Client {0} was not able to be added to the list of clients.", guid);
			}

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
