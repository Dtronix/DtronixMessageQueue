using System;
using System.Net.Sockets;

namespace DtronixMessageQueue.Socket {
	/// <summary>
	/// Base socket for all server and client sockets.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public abstract class SocketBase<TSession, TConfig>
		where TSession : SocketSession<TConfig>, new()
		where TConfig : SocketConfig {

		/// <summary>
		/// Mode that this socket is running as.
		/// </summary>
		public SocketMode Mode { get; }

		/// <summary>
		/// True if the socket is connected/listening.
		/// </summary>
		public abstract bool IsRunning { get; }

		/// <summary>
		/// This event fires when a connection has been established.
		/// </summary>
		public event EventHandler<SessionEventArgs<TSession, TConfig>> Connected;

		/// <summary>
		/// This event fires when a connection has been closed.
		/// </summary>
		public event EventHandler<SessionClosedEventArgs<TSession, TConfig>> Closed;
		
		/// <summary>
		/// Event called when a new session is created and is being setup but before the session is active.
		/// </summary>
		public event EventHandler<SessionEventArgs<TSession, TConfig>> SessionSetup;

		/// <summary>
		/// Configurations of this socket.
		/// </summary>
		public TConfig Config { get; }

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
		/// <param name="mode">Mode of that this socket is running in.</param>
		protected SocketBase(TConfig config, SocketMode mode) {
			Mode = mode;
			Config = config;
		}


		/// <summary>
		/// Method called when a session connects.
		/// </summary>
		/// <param name="session">Session that connected.</param>
		protected virtual void OnConnect(TSession session) {
			Connected?.Invoke(this, new SessionEventArgs<TSession, TConfig>(session));
		}


		/// <summary>
		/// Method called when a session closes.
		/// </summary>
		/// <param name="session">Session that closed.</param>
		/// <param name="reason">Reason for the closing of the session.</param>
		protected virtual void OnClose(TSession session, SocketCloseReason reason) {
			Closed?.Invoke(this, new SessionClosedEventArgs<TSession, TConfig>(session, reason));
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
		/// <returns>New session instance.</returns>
		protected virtual TSession CreateSession() {
	 		var session = new TSession();

			SessionSetup?.Invoke(this, new SessionEventArgs<TSession, TConfig>(session));
			session.Closed += (sender, args) => OnClose(session, args.CloseReason);
			return session;
		}
	}
}
