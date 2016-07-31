using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using NLog;

namespace DtronixMessageQueue {
	public  abstract class MQConnector : IDisposable {

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public const int ClientBufferSize = 1024 * 16;

		public class Connection {
			public MQMessage Message;
			public MQMailbox Mailbox;
			public Guid Id;
			public Socket Socket;
			public MQFrameBuilder FrameBuilder = new MQFrameBuilder(ClientBufferSize);
			public Semaphore WriterSemaphore = new Semaphore(1, 1);
			public SocketAsyncEventArgs SocketAsyncEvent;
		}

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

		protected Socket MainSocket; 
		protected bool IsRunning;
		public SocketAsyncEventArgsPool WritePool;
		protected SocketAsyncEventArgsPool ReadPool;

		private readonly Semaphore write_semaphore;

		protected BufferManager BufferManager;  // represents a large reusable set of buffers for all socket operations

		public event EventHandler<IncomingMessageEventArgs> OnIncomingMessage;

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
			var connection = e.UserToken as Connection;
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
					SendComplete(e);
					break;

				default:
					throw new ArgumentException("The last operation completed on the socket was not a receive or send");
			}
		}




		/// <summary>
		/// This method is invoked when an asynchronous receive operation completes. 
		/// If the remote host closed the connection, then the socket is closed.
		/// If data was received then the data is echoed back to the client.
		/// </summary>
		/// <param name="e"></param>
		protected void RecieveComplete(SocketAsyncEventArgs e) {
			var connection = e.UserToken as Connection;
			if (connection == null) {
				return;
			}

			if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success) {

				// If the bytes received is larger than the buffer, ignore this operation.
				if (e.BytesTransferred <= ClientBufferSize) {
					HandleRecieve(connection, e);
				}
				
				// Re-setup the receive async call.
				if (connection.Socket.ReceiveAsync(e) == false) {
					IoCompleted(this, e);
				}

			} else {
				CloseClientSocket(e);
			}
		}

		protected virtual void HandleRecieve(Connection connection, SocketAsyncEventArgs e) {
			if (connection.Message == null) {
				connection.Message = new MQMessage();
			}
			try {
				connection.FrameBuilder.Write(e.Buffer, e.Offset, e.BytesTransferred);
			} catch (InvalidDataException) {
				CloseClientSocket(e);
				return;
			}

			var frame_count = connection.FrameBuilder.Frames.Count;
			logger.Debug("Connector {0}: Parsed {1} frames.", connection.Id, frame_count);

			for (var i = 0; i < frame_count; i++) {
				var frame = connection.FrameBuilder.Frames.Dequeue();
				connection.Message.Add(frame);

				if (frame.FrameType == MQFrameType.EmptyLast || frame.FrameType == MQFrameType.Last) {
					
					connection.Mailbox.Enqueue(connection.Message);
					connection.Message = new MQMessage();

					OnIncomingMessage?.Invoke(this, new IncomingMessageEventArgs(connection.Mailbox, connection.Id));
				}
			}
		}

		/// <summary>
		/// Sends an array of data to the other end of the connection.
		/// </summary>
		/// <param name="connection">Connection to send data on.</param>
		/// <param name="data">Data to send.</param>
		/// <param name="offset">Starting offset of date in the buffer.</param>
		/// <param name="length">Amount of data in bytes to send.</param>
		/// <returns></returns>
		protected bool Send(Connection connection, byte[] data, int offset, int length) {
			connection.WriterSemaphore.WaitOne();
			var status = true;

			if (connection.Socket == null || connection.Socket.Connected == false) {
				return false;
			}

			SocketAsyncEventArgs args;

			// Try to get a new writer even arg.
			try {
				args = WritePool.Count > 0 ? WritePool.Pop() : CreateWriterEventArgs();
			} catch (Exception) {
				// The pool ran out of args between the check on the Count and the Pop call.
				args = CreateWriterEventArgs();
			}
			
			
			
			args.UserToken = connection;
			args.SetBuffer(data, offset, length);

			try {
				if (connection.Socket.SendAsync(args) == false) {
					IoCompleted(this, args);
				}
			} catch (ObjectDisposedException) {
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
			var connection = e.UserToken as Connection;
			if (connection == null) {
				return;
			}

			// Free this writer back to the pool.
			WritePool.Push(e);

			// Reset the waiter.
			connection.WriterSemaphore.Release(1);
			if (e.SocketError == SocketError.Success) {
				// Do nothing at this point.
			} else {
				CloseClientSocket(e);
			}
		}

		protected virtual void CloseClientSocket(SocketAsyncEventArgs e) {
			var client = e.UserToken as Connection;
			if (client == null) {
				return;
			}

			// close the socket associated with the client
			try {
				client.Socket.Shutdown(SocketShutdown.Send);
			} catch (Exception) {
				// ignored
				// throws if client process has already closed
			}
			client.Socket.Close();

			client.FrameBuilder.Dispose();

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
