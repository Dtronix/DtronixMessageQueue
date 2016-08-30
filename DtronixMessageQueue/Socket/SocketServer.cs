using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Socket {
	/// <summary>
	/// Base functionality for handling connection requests.
	/// </summary>
	/// <typeparam name="TSession">Session type.</typeparam>
	public class SocketServer<TSession> : SocketBase<TSession>
		where TSession : SocketSession, new() {

		/// <summary>
		/// Limits the number of active connections.
		/// </summary>
		private readonly Semaphore connection_limit;

		/// <summary>
		/// True if the server is listening and accepting connections.  False if the server is closed.
		/// </summary>
		public override bool IsRunning => is_stopped == false && (MainSocket?.IsBound ?? false);

		/// <summary>
		/// Dictionary of all connected clients.
		/// </summary>
		protected readonly ConcurrentDictionary<Guid, TSession> ConnectedSessions = new ConcurrentDictionary<Guid, TSession>();

		private bool is_stopped = false;

		/// <summary>
		/// Creates a socket server with the specified configurations.
		/// </summary>
		/// <param name="config">Configurations for this socket.</param>
		public SocketServer(SocketConfig config) : base(config) {
			connection_limit = new Semaphore(config.MaxConnections, config.MaxConnections);
		}

		/// <summary>
		/// Starts the server and begins listening for incoming connections.
		/// </summary>
		public void Start() {
			var ip = IPAddress.Parse(Config.Ip);
			var local_end_point = new IPEndPoint(ip, Config.Port);
			if (MainSocket != null && MainSocket.IsBound) {
				throw new InvalidOperationException("Server is already running.");
			}

			// create the socket which listens for incoming connections
			MainSocket = new System.Net.Sockets.Socket(local_end_point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			MainSocket.Bind(local_end_point);

			// start the server with a listen backlog.
			MainSocket.Listen(Config.ListenerBacklog);

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
				e.Completed += (sender, completed_e) => AcceptCompleted(completed_e);
			} else {
				// socket must be cleared since the context object is being reused
				e.AcceptSocket = null;
			}

			connection_limit.WaitOne();
			try {
				if (MainSocket.AcceptAsync(e) == false) {
					AcceptCompleted(e);
				}
			} catch (ObjectDisposedException) {
				// ignored
			}
			
		}

		/// <summary>
		/// Called by the socket when a new connection has been accepted.
		/// </summary>
		/// <param name="e">Event args for this event.</param>
		private void AcceptCompleted(SocketAsyncEventArgs e) {
			if (MainSocket.IsBound == false) {
				return;
			}

			e.AcceptSocket.NoDelay = true;

			var session = CreateSession(e.AcceptSocket);

			// Add event to remove this session from the active client list.
			session.Closed += RemoveClientEvent;

			// Add this session to the list of connected sessions.
			ConnectedSessions.TryAdd(session.Id, session);

			// Invoke the events.
			Task.Run(() => OnConnect(session));

			// Accept the next connection request
			StartAccept(e);
		}

		/// <summary>
		/// Event called to remove the disconnected session from the list of active connections.
		/// </summary>
		/// <param name="sender">Sender of the disconnection event.</param>
		/// <param name="e">Session events.</param>
		private void RemoveClientEvent(object sender, SessionClosedEventArgs<SocketSession> e) {
			TSession session_out;
			ConnectedSessions.TryRemove(e.Session.Id, out session_out);
			e.Session.Closed -= RemoveClientEvent;
		}

		/// <summary>
		/// Creates a frame with the specified bytes and the current configurations.
		/// </summary>
		/// <param name="bytes">Bytes to put in the frame.</param>
		/// <returns>Configured frame.</returns>
		public override MqFrame CreateFrame(byte[] bytes) {
			return new MqFrame(bytes, (MqSocketConfig)Config);
		}

		/// <summary>
		/// Creates a frame with the specified bytes and the current configurations.
		/// </summary>
		/// <param name="bytes">Bytes to put in the frame.</param>
		/// <param name="type">Type of frame to create.</param>
		/// <returns>Configured frame.</returns>
		public override MqFrame CreateFrame(byte[] bytes, MqFrameType type) {
			return new MqFrame(bytes, type, (MqSocketConfig)Config);
		}



		/// <summary>
		/// Terminates this server and notify all connected clients.
		/// </summary>
		public void Stop() {
			if (is_stopped) {
				return;
			}
			TSession[] sessions = new TSession[ConnectedSessions.Values.Count];
			ConnectedSessions.Values.CopyTo(sessions, 0);

			foreach (var session in sessions) {
				session.CloseConnection(SocketCloseReason.ServerClosing);
			}

			MainSocket.Close();
			is_stopped = true;
		}
	}

}
