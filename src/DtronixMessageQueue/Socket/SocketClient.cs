using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DtronixMessageQueue.Socket {

	/// <summary>
	/// Base functionality for all client connections to a remote server.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public class SocketClient<TSession, TConfig> : SessionHandler<TSession, TConfig>
		where TSession : SocketSession<TSession, TConfig>, new()
		where TConfig : SocketConfig {

		/// <summary>
		/// True if the client is connected to a server.
		/// </summary>
		public override bool IsRunning => MainSocket?.Connected ?? false;


		/// <summary>
		/// Session for this client.
		/// </summary>
		public TSession Session { get; private set; }

		/// <summary>
		/// Creates a socket client with the specified configurations.
		/// </summary>
		/// <param name="config">Configurations to use.</param>
		public SocketClient(TConfig config) : base(config, SocketMode.Client) {
		}

		/// <summary>
		/// Connects to the configured endpoint.
		/// </summary>
		public void Connect() {
			Connect(new IPEndPoint(IPAddress.Parse(Config.Ip), Config.Port));
		}

		

		/// <summary>
		/// Connects to the specified endpoint.
		/// </summary>
		/// <param name="end_point">Endpoint to connect to.</param>
		public void Connect(IPEndPoint end_point) {
			if (MainSocket != null && Session?.CurrentState != SocketSession<TSession, TConfig>.State.Closed) {
				throw new InvalidOperationException("Client is in the process of connecting.");
			}

			MainSocket = new System.Net.Sockets.Socket(end_point.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {
				NoDelay = true
			};

			bool timed_out = false;

			Timer connection_timer = null;

			connection_timer = new Timer(o => {
				timed_out = true;
				MainSocket.Close();
				connection_timer.Change(Timeout.Infinite, Timeout.Infinite);
				connection_timer.Dispose();

				OnClose(null, SocketCloseReason.TimeOut);
			});

			var event_arg = new SocketAsyncEventArgs {
				RemoteEndPoint = end_point
			};

			event_arg.Completed += (sender, args) => {
				if (timed_out) {
					return;
				}
				if (args.LastOperation == SocketAsyncOperation.Connect) {

					// Stop the timeout timer.
					connection_timer.Change(Timeout.Infinite, Timeout.Infinite);
					connection_timer.Dispose();

					Session = CreateSession(MainSocket);
					Session.Connected += (sndr, e) => OnConnect(Session);

					ConnectedSessions.TryAdd(Session.Id, Session);

					((ISetupSocketSession)Session).Start();
				}
			};

			

			MainSocket.ConnectAsync(event_arg);

			connection_timer.Change(Config.ConnectionTimeout, Timeout.Infinite);

		}

		protected override void OnClose(TSession session, SocketCloseReason reason) {
			MainSocket.Close();

			TSession sess_out;

			// If the session is null, the connection timed out while trying to connect.
			if (session != null) {
				ConnectedSessions.TryRemove(Session.Id, out sess_out);
			}

			base.OnClose(session, reason);
		}
	}
}
