using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using Amib.Threading;

namespace DtronixMessageQueue.Socket {
	/// <summary>
	/// Base socket for all server and client sockets.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public abstract class SessionHandler<TSession, TConfig>
		where TSession : SocketSession<TSession, TConfig>, new()
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
		/// Base read-write pool for the sessions to utilize.
		/// </summary>
		protected SmartThreadPool ThreadPool;

		/// <summary>
		/// True if the timeout timer is running.  False otherwise.
		/// </summary>
		protected bool TimeoutTimerRunning;

		/// <summary>
		/// Timer used to verify that the sessions are still connected.
		/// </summary>
		protected readonly Timer TimeoutTimer;

		/// <summary>
		/// Dictionary of all connected clients.
		/// </summary>
		protected readonly ConcurrentDictionary<Guid, TSession> ConnectedSessions = new ConcurrentDictionary<Guid, TSession>();

		/// <summary>
		/// Base constructor to all socket classes.
		/// </summary>
		/// <param name="config">Configurations for this socket.</param>
		/// <param name="mode">Mode of that this socket is running in.</param>
		protected SessionHandler(TConfig config, SocketMode mode) {
			TimeoutTimer = new Timer(TimeoutCallback);
			Mode = mode;
			Config = config;
		}


		/// <summary>
		/// Called by the timer to verify that the session is still connected.  If it has timed out, close it.
		/// </summary>
		/// <param name="state">Concurrent dictionary of the sessions.</param>
		protected virtual void TimeoutCallback(object state) {
			var timout_int = Config.PingTimeout;
			var timeout_time = DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 0, 0, timout_int));

			foreach (var session in ConnectedSessions.Values) {
				if (session.LastReceived < timeout_time) {
					session.Close(SocketCloseReason.TimeOut);
				}
			}
		}


		/// <summary>
		/// Method called when a session connects.
		/// </summary>
		/// <param name="session">Session that connected.</param>
		protected virtual void OnConnect(TSession session) {
			// Start the timeout timer if it is not already running.
			if (TimeoutTimerRunning == false) {
				TimeoutTimer.Change(0, Config.PingTimeout);
				TimeoutTimerRunning = true;
			}

			Connected?.Invoke(this, new SessionEventArgs<TSession, TConfig>(session));
		}


		/// <summary>
		/// Method called when a session closes.
		/// </summary>
		/// <param name="session">Session that closed.</param>
		/// <param name="reason">Reason for the closing of the session.</param>
		protected virtual void OnClose(TSession session, SocketCloseReason reason) {
			// If there are no clients connected, stop the timer.
			if (ConnectedSessions.IsEmpty) {
				TimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
				TimeoutTimerRunning = false;
			}

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

			ThreadPool = new SmartThreadPool(Config.ThreadPoolTimeout, Config.MaxWorkingThreads, Config.MinWorkingThreads);
		}

		/// <summary>
		/// Method called when new sessions are created.  Override to change behavior.
		/// </summary>
		/// <returns>New session instance.</returns>
		protected virtual TSession CreateSession(System.Net.Sockets.Socket socket) {
			var session = SocketSession<TSession, TConfig>.Create(socket, AsyncPool, Config, ThreadPool, this);
			
			SessionSetup?.Invoke(this, new SessionEventArgs<TSession, TConfig>(session));
			session.Closed += (sender, args) => OnClose(session, args.CloseReason);
			return session;
		}
	}
}
