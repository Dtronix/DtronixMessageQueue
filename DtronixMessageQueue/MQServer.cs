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
using NLog;

namespace DtronixMessageQueue {
	public class MQServer : MQConnector {

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		BufferManager buffer_manager;  // represents a large reusable set of buffers for all socket operations
		Socket listen_socket;            // the socket used to listen for incoming connection requests
										 // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
		SocketAsyncEventArgsPool read_write_pool;
		int num_connected_sockets;      // the total number of clients connected to the server 
		Semaphore max_number_accepted_clients;



		public const int ClientBufferSize = 1024 * 16;

		public class Client {
			public MQFrame Frame;
			public MQMessage Message;
			public MQMailbox Mailbox;
			public Guid Id;
			public Socket Socket;
			public byte[] Bytes = new byte[ClientBufferSize];
			public MQFrameBuilder FrameBuilder = new MQFrameBuilder(ClientBufferSize);
			public object WriteLock = new object();
			public SocketAsyncEventArgs SocketAsyncEvent;
		}



		public class Config {

			public int MaxConnections { get; set; } = 256;

			public int MinimumWorkers { get; set; } = 4;

			/// <summary>
			/// Maximum backlog for pending connections.
			/// The default value is 100.
			/// </summary>
			public int ListenerBacklog { get; set; } = 100;
		}

		private readonly Config configurations;

		private bool is_running;

		private readonly ConcurrentDictionary<Guid, Client> connected_clients = new ConcurrentDictionary<Guid, Client>();


		public MQServer(Config configurations) {
			this.configurations = configurations;

			// allocate buffers such that the maximum number of sockets can have one outstanding read and 
			//write posted to the socket simultaneously  
			buffer_manager = new BufferManager(ClientBufferSize * configurations.MaxConnections * 2, ClientBufferSize);

			read_write_pool = new SocketAsyncEventArgsPool(configurations.MaxConnections);
			max_number_accepted_clients = new Semaphore(configurations.MaxConnections, configurations.MaxConnections);

			// Allocates one large byte buffer which all I/O operations use a piece of.  This guards against memory fragmentation.
			buffer_manager.InitBuffer();

			// preallocate pool of SocketAsyncEventArgs objects
			SocketAsyncEventArgs read_write_event_arg;

			for (int i = 0; i < configurations.MaxConnections; i++) {
				//Pre-allocate a set of reusable SocketAsyncEventArgs
				read_write_event_arg = new SocketAsyncEventArgs();
				read_write_event_arg.Completed += IO_Completed;

				// assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
				buffer_manager.SetBuffer(read_write_event_arg);

				// add SocketAsyncEventArg to the pool
				read_write_pool.Push(read_write_event_arg);
			}

		}


		// Starts the server such that it is listening for 
		// incoming connection requests.    
		//
		// <param name="localEndPoint">The endpoint which the server will listening 
		// for connection requests on</param>
		public void Start(IPEndPoint local_end_point) {
			if (is_running) {
				throw new InvalidOperationException("Server is already running.");
			}

			// create the socket which listens for incoming connections
			listen_socket = new Socket(local_end_point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			listen_socket.Bind(local_end_point);

			// start the server with a listen backlog of 100 connections
			listen_socket.Listen(100);

			// post accepts on the listening socket
			StartAccept(null);
		}

		/// <summary>
		/// Begins an operation to accept a connection request from the client 
		/// </summary>
		/// <param name="acceptEventArg">The context object to use when issuing the accept operation on the server's listening socket</param>
		public void StartAccept(SocketAsyncEventArgs accept_event_arg) {
			if (accept_event_arg == null) {
				accept_event_arg = new SocketAsyncEventArgs();
				accept_event_arg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
			} else {
				// socket must be cleared since the context object is being reused
				accept_event_arg.AcceptSocket = null;
			}

			max_number_accepted_clients.WaitOne();
			bool will_raise_event = listen_socket.AcceptAsync(accept_event_arg);
			if (!will_raise_event) {
				ProcessAccept(accept_event_arg);
			}
		}

		// This method is the callback method associated with Socket.AcceptAsync 
		// operations and is invoked when an accept operation is complete
		//
		void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e) {
			ProcessAccept(e);
		}

		private void ProcessAccept(SocketAsyncEventArgs e) {
			if (is_running == false) {
				return;
			}

			Interlocked.Increment(ref num_connected_sockets);

			// Get the socket for the accepted client connection and put it into the 
			//ReadEventArg object user token
			SocketAsyncEventArgs read_event_args = read_write_pool.Pop();


			var guid = Guid.NewGuid();

			var client = new Client {
				Id = guid,
				Socket = e.AcceptSocket,
				Mailbox = new MQMailbox(),
				SocketAsyncEvent = e
			};

			read_event_args.UserToken = client;

			connected_clients.TryAdd(guid, client);

			// As soon as the client is connected, post a receive to the connection
			e.AcceptSocket.ReceiveAsync(read_event_args);

			// Accept the next connection request
			StartAccept(e);
		}

		public override void Stop() {
			base.Stop();

			Client[] clients = new Client[connected_clients.Values.Count];
			connected_clients.Values.CopyTo(clients, 0);

			foreach (Client client in clients) {
				client.Socket.DisconnectAsync(client.SocketAsyncEvent);
			}
		}

		public void Dispose() {
			if (is_running) {
				Stop();
			}
		}
	}

}
