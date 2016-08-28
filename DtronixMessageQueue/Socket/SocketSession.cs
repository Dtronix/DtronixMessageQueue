using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Socket {
	public abstract class SocketSession : IDisposable {

		public enum State : byte {
			Unknown,
			Connecting,
			Connected,
			Closing,
			Closed,
			Error
		}

		private SocketAsyncEventArgsPool args_pool;

		private ManualResetEventSlim write_reset;

		/// <summary>
		/// Id for this session
		/// </summary>
		public Guid Id { get; }

		/// <summary>
		/// State that this socket is in.  Can only perform most operations when the socket is in a Connected state.
		/// </summary>
		public State CurrentState { get; protected set; }

		/// <summary>
		/// Raw socket for this session.
		/// </summary>
		public System.Net.Sockets.Socket Socket {
			get { return socket; }
		}

		/// <summary>
		/// Last time the session received anything from the socket.
		/// </summary>
		public DateTime LastReceived => last_received;


		private SocketAsyncEventArgs send_args;

		private SocketAsyncEventArgs receive_args;

		private DateTime last_received;


		private System.Net.Sockets.Socket socket;

		/// <summary>
		/// This event fires when a connection has been established.
		/// </summary>
		public event EventHandler<SessionConnectedEventArgs<SocketSession>> Connected;

		/// <summary>
		/// This event fires when a connection has been shutdown.
		/// </summary>
		public event EventHandler<SessionClosedEventArgs<SocketSession>> Closed;



		protected SocketSession() {
			Id = Guid.NewGuid();
			CurrentState = State.Connecting;
		}

		internal static void Setup(SocketSession session, System.Net.Sockets.Socket socket, SocketAsyncEventArgsPool args_pool, SocketConfig configs) {
			session.args_pool = args_pool;
			session.send_args = args_pool.Pop();
			session.send_args.Completed += session.IoCompleted;
			session.receive_args = args_pool.Pop();
			session.receive_args.Completed += session.IoCompleted;


			session.socket = socket;
			session.write_reset = new ManualResetEventSlim(true);

			if(configs.SendTimeout > 0)
				socket.SendTimeout = configs.SendTimeout;

			if (configs.SendAndReceiveBufferSize > 0)
				socket.ReceiveBufferSize = configs.SendAndReceiveBufferSize;

			if (configs.SendAndReceiveBufferSize > 0)
				socket.SendBufferSize = configs.SendAndReceiveBufferSize;

			socket.NoDelay = true;
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

			// Start receiving data.
			socket.ReceiveAsync(session.receive_args);

			session.CurrentState = State.Connected;
		}

		protected void OnConnected() {
			Connected?.Invoke(this, new SessionConnectedEventArgs<SocketSession>(this));
		}

		protected void OnDisconnected(SocketCloseReason reason) {
			Closed?.Invoke(this, new SessionClosedEventArgs<SocketSession>(this, reason));
		}

		/// <summary>
		/// This method is called whenever a receive or send operation is completed on a socket 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
		protected virtual void IoCompleted(object sender, SocketAsyncEventArgs e) {
			// determine which type of operation just completed and call the associated handler
			switch (e.LastOperation) {
				case SocketAsyncOperation.Connect:
					OnConnected();
					break;

				case SocketAsyncOperation.Disconnect:
					CloseConnection(SocketCloseReason.ClientClosing);
					break;

				case SocketAsyncOperation.Receive:
					RecieveComplete(e);

					break;

				case SocketAsyncOperation.Send:
					SendComplete(e);
					break;

				default:
					throw new ArgumentException("The last operation completed on the socket was not a receive, send connect or disconnect.");
			}
		}




		internal void Send(byte[] buffer, int offset, int length) {
			if (Socket == null || Socket.Connected == false) {
				return;
			}

			write_reset.Wait();
			write_reset.Reset();
			Buffer.BlockCopy(buffer, offset, send_args.Buffer, send_args.Offset, length);

			send_args.SetBuffer(send_args.Offset, length);

			try {
				if (Socket.SendAsync(send_args) == false) {
					IoCompleted(this, send_args);
				}
			} catch (ObjectDisposedException) {
				CloseConnection(SocketCloseReason.SocketError);
			}
		}


		/// <summary>
		/// This method is invoked when an asynchronous send operation completes.  
		/// The method issues another receive on the socket to read any additional data sent from the client
		/// </summary>
		/// <param name="e"></param>
		private void SendComplete(SocketAsyncEventArgs e) {
			if (e.SocketError != SocketError.Success) {
				CloseConnection(SocketCloseReason.SocketError);
			}
			write_reset.Set();
		}

		/// <summary>
		/// This method is invoked when an asynchronous receive operation completes. 
		/// If the remote host closed the connection, then the socket is closed.
		/// If data was received then the data is echoed back to the client.
		/// </summary>
		/// <param name="e"></param>
		protected void RecieveComplete(SocketAsyncEventArgs e) {
			if (CurrentState == State.Closing) {
				return;
			}
			if (e.BytesTransferred == 0 && CurrentState == State.Connected) {
				CurrentState = State.Closing;
				return;
			}
			if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success) {

				// Update the last time this session was active.
				last_received = DateTime.UtcNow;

				// If the bytes received is larger than the buffer, ignore this operation.
				if (e.BytesTransferred > MqFrame.MaxFrameSize + MqFrame.HeaderLength) {
					CloseConnection(SocketCloseReason.SocketError);
				}

				// Create a copy of these bytes.
				var buffer = new byte[e.BytesTransferred];

				Buffer.BlockCopy(e.Buffer, e.Offset, buffer, 0, e.BytesTransferred);

				HandleIncomingBytes(buffer);
				//previous_bytes = buffer;


				try {
					// Re-setup the receive async call.
					if (Socket.ReceiveAsync(e) == false) {
						IoCompleted(this, e);
					}
				} catch (ObjectDisposedException) {
					CloseConnection(SocketCloseReason.SocketError);
				}


			} else {
				CloseConnection(SocketCloseReason.SocketError);
			}
		}

		protected abstract void HandleIncomingBytes(byte[] buffer);

		public virtual void CloseConnection(SocketCloseReason reason) {
			// If this session has already been closed, nothing more to do.
			if (CurrentState == State.Closed) {
				return;
			}
			// close the socket associated with the client
			try {
				Socket.Close(1000);
			} catch (Exception) {
				// ignored
			}

			// Free the SocketAsyncEventArg so they can be reused by another client
			args_pool.Push(send_args);
			args_pool.Push(receive_args);

			send_args.Completed -= IoCompleted;
			receive_args.Completed -= IoCompleted;


			// Notify the session has been closed.
			OnDisconnected(reason);

			CurrentState = State.Closed;
		}

		public void Dispose() {
			if (CurrentState == State.Connected) {
				CloseConnection(SocketCloseReason.ClientClosing);
			}
		}
	}
}
