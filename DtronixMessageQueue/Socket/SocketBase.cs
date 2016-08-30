using System;
using System.Net.Sockets;

namespace DtronixMessageQueue.Socket {
	/// <summary>
	/// Base socket for all server and client sockets.
	/// </summary>
	/// <typeparam name="TSession">Session type for this</typeparam>
	public abstract class SocketBase<TSession>
		where TSession : SocketSession, new() {

		/// <summary>
		/// True if the socket is connected/listening.
		/// </summary>
		public abstract bool IsRunning { get; }

		/// <summary>
		/// This event fires when a connection has been established.
		/// </summary>
		public event EventHandler<SessionConnectedEventArgs<TSession>> Connected;

		/// <summary>
		/// This event fires when a connection has been closed.
		/// </summary>
		public event EventHandler<SessionClosedEventArgs<TSession>> Closed;

		/// <summary>
		/// Configurations of this socket.
		/// </summary>
		public SocketConfig Config { get; }

		/// <summary>
		/// Main socket used by the child class for connection or for the listening of incoming connections.
		/// </summary>
		protected System.Net.Sockets.Socket MainSocket; 

		/// <summary>
		/// Pool of async args for sessions to use.
		/// </summary>
		protected SocketAsyncEventArgsPool AsyncPool;

		/// <summary>
		/// Reusable buffer set for all the connected sessions.
		/// </summary>
		protected BufferManager BufferManager;  // represents a large reusable set of buffers for all socket operations


		/// <summary>
		/// Base constructor to all socket classes.
		/// </summary>
		/// <param name="config">Configurations for this socket.</param>
		protected SocketBase(SocketConfig config) {
			Config = config;
		}


		/// <summary>
		/// Method called when a session connects.
		/// </summary>
		/// <param name="session">Session that connected.</param>
		protected virtual void OnConnect(TSession session) {
			Connected?.Invoke(this, new SessionConnectedEventArgs<TSession>(session));
		}


		/// <summary>
		/// Method called when a session closes.
		/// </summary>
		/// <param name="session">Session that closed.</param>
		/// <param name="reason">Reason for the closing of the session.</param>
		protected virtual void OnClose(TSession session, SocketCloseReason reason) {
			Closed?.Invoke(this, new SessionClosedEventArgs<TSession>(session, reason));
		}

		/// <summary>
		/// Called by the constructor of the sub-class to set all configurations.
		/// </summary>
		protected void Setup() {
			// allocate buffers such that the maximum number of sockets can have one outstanding read and 
			//write posted to the socket simultaneously  
			BufferManager = new BufferManager(Config.SendAndReceiveBufferSize * Config.MaxConnections * 2, Config.SendAndReceiveBufferSize);

			// Allocates one large byte buffer which all I/O operations use a piece of.  This guards against memory fragmentation.
			BufferManager.InitBuffer();

			// preallocate pool of SocketAsyncEventArgs objects
			AsyncPool = new SocketAsyncEventArgsPool(Config.MaxConnections * 2);

			for (var i = 0; i < Config.MaxConnections*2; i++) {
				//Pre-allocate a set of reusable SocketAsyncEventArgs
				var event_arg = new SocketAsyncEventArgs();

				// assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
				BufferManager.SetBuffer(event_arg);

				// add SocketAsyncEventArg to the pool
				AsyncPool.Push(event_arg);
			}
		}

		/// <summary>
		/// Method called when new sessions are created.  Override to change behavior.
		/// </summary>
		/// <param name="socket">Socket this session will be using.</param>
		/// <returns>New session instance.</returns>
		protected virtual TSession CreateSession(System.Net.Sockets.Socket socket) {
			var session = new TSession();
			SocketSession.Setup(session, socket, AsyncPool, Config);
			session.Closed += (sender, args) => OnClose(session, args.CloseReason);
			return session;
		}


		public abstract MqFrame CreateFrame(byte[] bytes);

		public abstract MqFrame CreateFrame(byte[] bytes, MqFrameType type);
	}
}
