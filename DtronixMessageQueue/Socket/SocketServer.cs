using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace DtronixMessageQueue.Socket {
	public class SocketServer<TSession> : SocketBase<TSession>
		where TSession : SocketSession, new() {

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		private readonly Semaphore connection_limit;

		private readonly SocketConfig configurations;


		private readonly ConcurrentDictionary<Guid, TSession> connected_clients = new ConcurrentDictionary<Guid, TSession>();


		public SocketServer(SocketConfig configurations) : base(configurations) {
			this.configurations = configurations;

			connection_limit = new Semaphore(configurations.MaxConnections, configurations.MaxConnections);

		}

		public void Start() {
			Start(new IPEndPoint(IPAddress.Parse(Config.Ip), Config.Port));
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
			MainSocket = new System.Net.Sockets.Socket(local_end_point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			MainSocket.Bind(local_end_point);

			// start the server with a listen backlog.
			MainSocket.Listen(configurations.ListenerBacklog);

			// post accepts on the listening socket
			StartAccept(null);
		}

		/// <summary>
		/// Begins an operation to accept a connection request from the client 
		/// </summary>
		/// <param name="e">The context object to use when issuing the accept operation on the server's listening socket</param>
		private void StartAccept(SocketAsyncEventArgs e) {
			if (e == null) {
				e = new SocketAsyncEventArgs();
				e.Completed += AcceptEventArg_Completed;
			} else {
				// socket must be cleared since the context object is being reused
				e.AcceptSocket = null;
			}

			connection_limit.WaitOne();
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
			//SocketAsyncEventArgs read_event_args = AsyncPool.Pop();

			var session = CreateSession(e.AcceptSocket);

			connected_clients.TryAdd(session.Id, session);

			// Invoke the events.
			Task.Run(() => OnConnect(session));

			// As soon as the client is connected, post a receive to the connection
			//e.AcceptSocket.ReceiveAsync(read_event_args);

			// Accept the next connection request
			StartAccept(e);
		}


		public void Stop() {
			TSession[] sessions = new TSession[connected_clients.Values.Count];
			connected_clients.Values.CopyTo(sessions, 0);

			foreach (var session in sessions) {
				session.CloseConnection();
			}
		}
	}

}
