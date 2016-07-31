using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public  abstract class MQConnector : IDisposable {

		public event EventHandler<IncomingMessageEventArgs> OnIncomingMessage;
		/// <summary>
		/// This method is called whenever a receive or send operation is completed on a socket 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
		protected void IO_Completed(object sender, SocketAsyncEventArgs e) {
			// determine which type of operation just completed and call the associated handler
			switch (e.LastOperation) {
				case SocketAsyncOperation.Receive:
					ProcessReceive(e);
					break;
				case SocketAsyncOperation.Send:
					ProcessSend(e);
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
		protected void ProcessReceive(SocketAsyncEventArgs e) {
			var client = e.UserToken as MQServer.Client;
			if (client == null) {
				return;
			}


			if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success) {


				if (client.Message == null) {
					client.Message = new MQMessage();
				}
				try {
					client.FrameBuilder.Write(e.Buffer, e.Offset, e.BytesTransferred);
				} catch (InvalidDataException ex) {
					CloseClientSocket(e);
					return;
				}

				var frame_count = client.FrameBuilder.Frames.Count;

				for (var i = 0; i < frame_count; i++) {
					var frame = client.FrameBuilder.Frames.Dequeue();
					client.Message.Add(frame);

					if (frame.FrameType == MQFrameType.EmptyLast || frame.FrameType == MQFrameType.Last) {
						client.Mailbox.Enqueue(client.Message);
						client.Message = new MQMessage();
						OnIncomingMessage?.Invoke(this, new IncomingMessageEventArgs(client.Mailbox, client.Id));
					}
				}

				client.Socket.ReceiveAsync(e);


				//echo the data received back to the client
				//e.SetBuffer(e.Offset, e.BytesTransferred);


				//bool will_raise_event = client.Socket.SendAsync(e);

				//if (!will_raise_event) {
				//	ProcessSend(e);
				//}

			} else {
				CloseClientSocket(e);
			}
		}

		/// <summary>
		/// This method is invoked when an asynchronous send operation completes.  
		/// The method issues another receive on the socket to read any additional data sent from the client
		/// </summary>
		/// <param name="e"></param>
		protected void ProcessSend(SocketAsyncEventArgs e) {
			var client = e.UserToken as MQServer.Client;
			if (client == null) {
				return;
			}

			if (e.SocketError == SocketError.Success) {
				// Do nothing at this point.
			} else {
				CloseClientSocket(e);
			}
		}

		protected void CloseClientSocket(SocketAsyncEventArgs e) {
			var client = e.UserToken as MQServer.Client;
			if (client == null) {
				return;
			}

			// close the socket associated with the client
			try {
				client.Socket.Shutdown(SocketShutdown.Send);
			}
			// throws if client process has already closed
			catch (Exception) { }
			client.Socket.Close();

			client.FrameBuilder.Dispose();

			MQServer.Client cli;
			if (connected_clients.TryRemove(client.Id, out cli) == false) {
				logger.Fatal("Client {0} was not able to be removed from the list of clients.", client.Id);
			}

			// decrement the counter keeping track of the total number of clients connected to the server
			Interlocked.Decrement(ref num_connected_sockets);
			max_number_accepted_clients.Release();

			// Free the SocketAsyncEventArg so they can be reused by another client
			read_write_pool.Push(e);
		}


		public virtual void Stop() {
			if (is_running == false) {
				throw new InvalidOperationException("Server is not running.");
			}
			is_running = false;
		}

		public void Dispose() {
			
		}
	}
}
