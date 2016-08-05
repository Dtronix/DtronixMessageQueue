using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using NLog;

namespace DtronixMessageQueue {
	public  abstract class MQConnector : IDisposable {

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();


		public int ClientBufferSize { get; } = 1024 * 16;

		public MQPostmaster Postmaster { get; protected set; }


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

		/// <summary>
		/// This event fires when data is finished sending on the socket.
		/// </summary>
		public event EventHandler<SocketAsyncEventArgs> DataSent;


		public event EventHandler<IncomingMessageEventArgs> InboxMessage;

		protected Socket MainSocket; 
		protected bool IsRunning;

		protected SocketAsyncEventArgsPool ReadPool;
		protected SocketAsyncEventArgsPool WritePool;

		protected BufferManager BufferManager;  // represents a large reusable set of buffers for all socket operations

		protected void OnConnected(SocketAsyncEventArgs e) {
			Connected?.Invoke(this, e);
		}

		protected void OnDisconnected(SocketAsyncEventArgs e) {
			Disconnected?.Invoke(this, e);
		}

		protected void OnDataReceived(SocketAsyncEventArgs e) {
			DataReceived?.Invoke(this, e);
		}

		protected void OnDataSent(SocketAsyncEventArgs e) {
			DataSent?.Invoke(this, e);
		}

		protected MQConnector(int concurrent_reads, int concurrent_writes) {

			// Setup the postmaster and the threads associated with it.
			Postmaster = new MQPostmaster(this);

			// allocate buffers such that the maximum number of sockets can have one outstanding read and 
			//write posted to the socket simultaneously  
			// Add the plus one per connection to detect if a whole empty buffer is sent to OnReceive.
			BufferManager = new BufferManager((ClientBufferSize + 1) * concurrent_reads, ClientBufferSize + 1);

			// Allocates one large byte buffer which all I/O operations use a piece of.  This guards against memory fragmentation.
			BufferManager.InitBuffer();

			// preallocate pool of SocketAsyncEventArgs objects
			ReadPool = new SocketAsyncEventArgsPool(concurrent_reads);

			for (var i = 0; i < concurrent_reads; i++) {
				//Pre-allocate a set of reusable SocketAsyncEventArgs
				var r_event_arg = new SocketAsyncEventArgs();

				// assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
				BufferManager.SetBuffer(r_event_arg);

				r_event_arg.Completed += IoCompleted;

				// add SocketAsyncEventArg to the pool
				ReadPool.Push(r_event_arg);
			}

			// Preallocate the writing pool
			WritePool = new SocketAsyncEventArgsPool(concurrent_writes);

			for (var i = 0; i < concurrent_writes; i++) {
				//Pre-allocate a set of reusable SocketAsyncEventArgs
				var w_event_arg = CreateWriterEventArgs();

				// add SocketAsyncEventArg to the pool
				WritePool.Push(w_event_arg);
			}

			logger.Debug("MQConnector started with {0} readers and {1} writers", concurrent_reads, concurrent_writes);
		}

		protected MQConnection CreateConnection() {
			var connection = new MQConnection(this);
			connection.Mailbox.IncomingMessage += (sender, args) => {
				InboxMessage?.Invoke(this, new IncomingMessageEventArgs(connection));
			};

			return connection;
		}

		protected SocketAsyncEventArgs CreateWriterEventArgs() {
			var w_event_arg = new SocketAsyncEventArgs();
			w_event_arg.Completed += IoCompleted;

			return w_event_arg;
		}

		/// <summary>
		/// This method is called whenever a receive or send operation is completed on a socket 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
		protected virtual void IoCompleted(object sender, SocketAsyncEventArgs e) {
			var connection = e.UserToken as MQConnection;
			if (connection != null) {
				logger.Debug("Connector {0}: Completed {1} Operation.", connection.Id, e.LastOperation);
			}
			// determine which type of operation just completed and call the associated handler
			switch (e.LastOperation) {
				case SocketAsyncOperation.Connect:
					Connected?.Invoke(this, e);
					break;

				case SocketAsyncOperation.Disconnect:
					Disconnected?.Invoke(this, e);
					break;

				case SocketAsyncOperation.Receive:
					RecieveComplete(e);

					break;

				case SocketAsyncOperation.Send:
					semaphore.Release(1);
					DataSent?.Invoke(this, e);
					SendComplete(e);
					break;

				default:
					logger.Error("Connector {0}: The last operation completed on the socket was not a receive, send connect or disconnect.", connection?.Id);
					throw new ArgumentException("The last operation completed on the socket was not a receive, send connect or disconnect.");
			}
		}




		/// <summary>
		/// This method is invoked when an asynchronous receive operation completes. 
		/// If the remote host closed the connection, then the socket is closed.
		/// If data was received then the data is echoed back to the client.
		/// </summary>
		/// <param name="e"></param>
		protected void RecieveComplete(SocketAsyncEventArgs e) {
			var connection = e.UserToken as MQConnection;
			if (connection == null) {
				return;
			}

			if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success) {

				// If the bytes received is larger than the buffer, ignore this operation.
				if (e.BytesTransferred <= ClientBufferSize) {

					// Create a copy of these bytes.
					var buffer = new byte[e.BytesTransferred];

					Buffer.BlockCopy(e.Buffer, e.Offset, buffer, 0, e.BytesTransferred);

		
					connection.Mailbox.EnqueueIncomingBuffer(buffer);
					//previous_bytes = buffer;
				}

				try {
					// Re-setup the receive async call.
					if (connection.Socket.ReceiveAsync(e) == false) {
						logger.Warn("Connector {0}: Data received synchronously.", connection.Id);
						IoCompleted(this, e);
					}
				} catch (ObjectDisposedException ex) {
					logger.Error(ex, "Connector {0}: Exception on SendAsync.", connection.Id);
					CloseConnection(e);
				}


			} else {
				logger.Error("Connector {0}: Socket error: {1}", connection?.Id, e.SocketError);
				CloseConnection(e);
			}
		}

		/// <summary>
		/// Sends an array of data to the other end of the connection.
		/// </summary>
		/// <param name="connection">MQConnection to send data on.</param>
		/// <param name="collection"></param>
		/// <returns></returns>
		public bool Send(MQConnection connection, BlockingCollection<byte[]> collection ) {
			byte[] buffer;
			while(collection.TryTake(out buffer)) {
				Send(connection, buffer, 0, buffer.Length);
			}

			return true;
		}

		private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

		/// <summary>
		/// Sends an array of data to the other end of the connection.
		/// </summary>
		/// <param name="connection">MQConnection to send data on.</param>
		/// <param name="data">Data to send.</param>
		/// <param name="offset">Starting offset of date in the buffer.</param>
		/// <param name="length">Amount of data in bytes to send.</param>
		/// <returns></returns>
		protected bool Send(MQConnection connection, byte[] data, int offset, int length) {
			semaphore.Wait();
			var status = true;

			if (connection.Socket == null || connection.Socket.Connected == false) {
				return false;
			}

			SocketAsyncEventArgs args;

			// Try to get a new writer even arg.
			try {
				args = WritePool.Count > 0 ? WritePool.Pop() : CreateWriterEventArgs();
			} catch (Exception) {
				logger.Warn("Connector {0}: Writer pool ran out of EventArgs between the check on the Count and the Pop call.", connection.Id);
				// The pool ran out of args between the check on the Count and the Pop call.
				args = CreateWriterEventArgs();
			}
			

			args.UserToken = connection;
			args.SetBuffer(data, offset, length);

			try {
				if (connection.Socket.SendAsync(args) == false) {
					logger.Warn("Connector {0}: Data sent synchronously.", connection.Id);
					IoCompleted(this, args);
				}
			} catch (ObjectDisposedException ex) {
				logger.Error(ex, "Connector {0}: Exception on SendAsync.", connection.Id);
				WritePool.Push(args);
				status = false;
			}

			return status;
		}

		/// <summary>
		/// This method is invoked when an asynchronous send operation completes.  
		/// The method issues another receive on the socket to read any additional data sent from the client
		/// </summary>
		/// <param name="e"></param>
		protected void SendComplete(SocketAsyncEventArgs e) {
			var connection = e.UserToken as MQConnection;
			if (connection == null) {
				return;
			}


			// Free this writer back to the pool.
			WritePool.Push(e);

			if (e.Buffer.Length != e.BytesTransferred) {
				
			}

			// Reset the waiter.
			
			if (e.SocketError == SocketError.Success) {
				// Do nothing at this point.
			} else {
				logger.Error("Connector {0}: Socket error: {1}", connection.Id, e.SocketError);
				CloseConnection(e);
			}
		}

		protected virtual void CloseConnection(SocketAsyncEventArgs e) {
			var connection = e.UserToken as MQConnection;
			if (connection == null) {
				return;
			}

			// close the socket associated with the client
			try {
				connection.Socket.Shutdown(SocketShutdown.Send);
			} catch (Exception ex) {
				logger.Error(ex, "Connector {0}: MQConnection is already closed.", connection.Id);
				// ignored
				// throws if client process has already closed
			}
			connection.Socket.Close();

			connection.FrameBuilder.Dispose();

			// Free the SocketAsyncEventArg so they can be reused by another client
			ReadPool.Push(e);
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
