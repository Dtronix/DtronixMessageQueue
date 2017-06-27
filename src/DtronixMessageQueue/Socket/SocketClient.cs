using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Socket
{

	/// <summary>
	/// Base functionality for all client connections to a remote server.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public class SocketClient<TSession, TConfig> : SessionHandler<TSession, TConfig>
		where TSession : SocketSession<TSession, TConfig>, new()
		where TConfig : SocketConfig
	{

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
		public SocketClient(TConfig config) : base(config, SocketMode.Client)
		{
		}

		/// <summary>
		/// Connects to the configured endpoint.
		/// </summary>
		public void Connect()
		{
			Connect(new IPEndPoint(IPAddress.Parse(Config.Ip), Config.Port));
		}

		/// <summary>
		/// Task which will run when a connection times out.
		/// </summary>
		private Task connection_timeout_task;

		/// <summary>
		/// Cancellation token to cancel the timeout event for connections.
		/// </summary>
		private CancellationTokenSource connection_timeout_cancellation;

		/// <summary>
		/// Connects to the specified endpoint.
		/// </summary>
		/// <param name="end_point">Endpoint to connect to.</param>
		public void Connect(IPEndPoint end_point)
		{
			if (MainSocket != null && Session?.CurrentState != SocketSession<TSession, TConfig>.State.Closed)
			{
				throw new InvalidOperationException("Client is in the process of connecting.");
			}

			MainSocket = new System.Net.Sockets.Socket(end_point.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
			{
				NoDelay = true
			};

			// Set to true if the client connection either timed out or was canceled.
			bool timed_out = false;
			connection_timeout_cancellation = new CancellationTokenSource();

			connection_timeout_task = new Task(async () =>
			{
				try
				{
					await Task.Delay(Config.ConnectionTimeout, connection_timeout_cancellation.Token);
				}
				catch
				{
					return;
				}

				timed_out = true;
				OnClose(null, SocketCloseReason.TimeOut);
				MainSocket.Close();
			});





			var event_arg = new SocketAsyncEventArgs
			{
				RemoteEndPoint = end_point
			};

			event_arg.Completed += (sender, args) => {
				if (timed_out)
				{
					return;
				}
				if (args.LastOperation == SocketAsyncOperation.Connect)
				{

					// Stop the timeout timer.
					connection_timeout_cancellation.Cancel();

					Session = CreateSession(MainSocket);
					Session.Connected += (sndr, e) => OnConnect(Session);

					ConnectedSessions.TryAdd(Session.Id, Session);

					((ISetupSocketSession)Session).Start();
				}
			};



			MainSocket.ConnectAsync(event_arg);

			connection_timeout_task.Start();


		}

		protected override void OnClose(TSession session, SocketCloseReason reason)
		{
			MainSocket.Close();

			TSession sess_out;

			// If the session is null, the connection timed out while trying to connect.
			if (session != null)
			{
				ConnectedSessions.TryRemove(Session.Id, out sess_out);
			}

			base.OnClose(session, reason);
		}
	}
}
