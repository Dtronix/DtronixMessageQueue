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
		private readonly Semaphore client_semaphore;

		public class Config {

			public int MaxConnections { get; set; } = 1;

			/// <summary>
			/// Maximum backlog for pending connections.
			/// The default value is 100.
			/// </summary>
			public int ListenerBacklog { get; set; } = 100;
		}

		private readonly Config configurations;

		private readonly ConcurrentDictionary<Guid, Connection> connected_clients = new ConcurrentDictionary<Guid, Connection>();


		public MQServer(Config configurations) : base(configurations.MaxConnections, configurations.MaxConnections) {
			this.configurations = configurations;

			client_semaphore = new Semaphore(configurations.MaxConnections, configurations.MaxConnections);

		}


		// Starts the server such that it is listening for 
		// incoming connection requests.    
		//
		// <param name="localEndPoint">The endpoint which the server will listening 
		// for connection requests on</param>
		public void Start(IPEndPoint local_end_point) {
			if (IsRunning) {
				throw new InvalidOperationException("Server is already running.");
			}

			IsRunning = true;

			// create the socket which listens for incoming connections
			MainSocket = new Socket(local_end_point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			MainSocket.Bind(local_end_point);

			// start the server with a listen backlog of 100 connections
			MainSocket.Listen(100);

			// post accepts on the listening socket
			StartAccept(null);
		}

		/// <summary>
		/// Begins an operation to accept a connection request from the client 
		/// </summary>
		/// <param name="acceptEventArg">The context object to use when issuing the accept operation on the server's listening socket</param>
		public void StartAccept(SocketAsyncEventArgs e) {
			if (e == null) {
				e = new SocketAsyncEventArgs();
				e.Completed += AcceptEventArg_Completed;
			} else {
				// socket must be cleared since the context object is being reused
				e.AcceptSocket = null;
			}

			client_semaphore.WaitOne();
			if (MainSocket.AcceptAsync(e) == false) {
				logger.Warn("Server: Client accepted synchronously.");
				AcceptCompleted(e);
			}
		}

		// This method is the callback method associated with Socket.AcceptAsync 
		// operations and is invoked when an accept operation is complete
		//
		void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e) {
			AcceptCompleted(e);
		}

		private void AcceptCompleted(SocketAsyncEventArgs e) {
			if (IsRunning == false) {
				return;
			}

			e.AcceptSocket.NoDelay = true;

			// Get the socket for the accepted client connection and put it into the 
			//ReadEventArg object user token
			SocketAsyncEventArgs read_event_args = ReadPool.Pop();


			var guid = Guid.NewGuid();

			var connection = new Connection {
				Id = guid,
				Socket = e.AcceptSocket,
				SocketAsyncEvent = e,
				Connector = this
			};

			connection.Mailbox = new MQMailbox(connection);

			read_event_args.UserToken = connection;

			connected_clients.TryAdd(guid, connection);

			// Invoke the events.
			OnConnected(e);

			// As soon as the client is connected, post a receive to the connection
			e.AcceptSocket.ReceiveAsync(read_event_args);

			// Accept the next connection request
			StartAccept(e);
		}

		public void CloseConnection(Connection connection) {
			CloseConnection(connection.SocketAsyncEvent);
		}


		protected override void CloseConnection(SocketAsyncEventArgs e) {
			var connection = e.UserToken as Connection;
			if (connection == null) {
				return;
			}

			base.CloseConnection(e);

			Connection cli;
			if (connected_clients.TryRemove(connection.Id, out cli) == false) {
				logger.Fatal("Connection {0} was not able to be removed from the list of clients.", connection.Id);
			}

			client_semaphore.Release();
		}

		public override void Stop() {
			base.Stop();

			Connection[] connections = new Connection[connected_clients.Values.Count];
			connected_clients.Values.CopyTo(connections, 0);

			foreach (Connection client in connections) {
				client.Socket.DisconnectAsync(client.SocketAsyncEvent);
			}
		}
	}

}
