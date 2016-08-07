using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

//using NLog;

namespace DtronixMessageQueue {
	public abstract class MqConnector : IDisposable {
		/// <summary>
		/// This event fires when a connection has been shutdown.
		/// </summary>
		public event EventHandler<SocketAsyncEventArgs> Disconnected;

		/// <summary>
		/// This event fires when data is received on the socket.
		/// </summary>
		public event EventHandler<SocketAsyncEventArgs> DataReceived;

		/// <summary>
		/// This event fires when the inbox has a message to read.
		/// </summary>
		public event EventHandler<IncomingMessageEventArgs> InboxMessage;

		/// <summary>
		/// This event fires when a connection has been established.
		/// </summary>
		public event EventHandler Connected;

		public bool IsRunning { get; protected set; }


		protected MqConnector() {
			// Setup the postmaster and the threads associated with it.
			//postmaster = new MqPostmaster(this);
		}

		protected void OnConnected() {
			Connected?.Invoke(this, new EventArgs());
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

		public void Send(MqSession session, byte[] buffer, int offset, int count) {
			session.SocketSession.TrySend(new ArraySegment<byte>(buffer, offset, count));
		}


		public void Dispose() {
			if (IsRunning) {
				Stop();
			}
		}
	}
}