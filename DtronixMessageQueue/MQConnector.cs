using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
//using NLog;

namespace DtronixMessageQueue {
	public  abstract class MqConnector : IDisposable {

		/// <summary>
		/// This event fires when a connection has been established.
		/// </summary>
		public event EventHandler<SocketAsyncEventArgs> Connected;

		/// <summary>
		/// This event fires when a connection has been shutdown.
		/// </summary>
		public event EventHandler<SocketAsyncEventArgs> Disconnected;

		/// <summary>
		/// This event fires when data is received on the socket.
		/// </summary>
		public event EventHandler<SocketAsyncEventArgs> DataReceived;

		public bool IsRunning { get; protected set; }

		public int ClientBufferSize { get; } = 1024 * 16;

		public MqPostmaster Postmaster { get; protected set; }

		public event EventHandler<IncomingMessageEventArgs> InboxMessage;

		protected MqConnector(int concurrent_reads, int concurrent_writes) {

			// Setup the postmaster and the threads associated with it.
			Postmaster = new MqPostmaster(this);

			//logger.Debug("MqConnector started with {0} readers and {1} writers", concurrent_reads, concurrent_writes);
		}

		protected void OnConnected(SocketAsyncEventArgs e) {
			Connected?.Invoke(this, e);
		}

		protected void OnDisconnected(SocketAsyncEventArgs e) {
			Disconnected?.Invoke(this, e);
		}

		protected void OnDataReceived(SocketAsyncEventArgs e) {
			DataReceived?.Invoke(this, e);
		}

		public virtual void Stop() {
			if (IsRunning == false) {
				throw new InvalidOperationException("Server is not running.");
			}
			IsRunning = false;
		}

		


		public void Dispose() {
			if (IsRunning) {
				Stop();
			}
		}
	}
}
