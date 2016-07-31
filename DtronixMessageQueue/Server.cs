using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	class Server {
		private int num_connections;   // the maximum number of connections the sample is designed to handle simultaneously 
		private int receive_buffer_size;// buffer size to use for each socket I/O operation 
		BufferManager buffer_manager;  // represents a large reusable set of buffers for all socket operations
		const int OpsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
		Socket listen_socket;            // the socket used to listen for incoming connection requests
										// pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
		SocketAsyncEventArgsPool read_write_pool;
		int total_bytes_read;           // counter of the total # bytes received by the server
		int num_connected_sockets;      // the total number of clients connected to the server 
		Semaphore max_number_accepted_clients;

		// Create an uninitialized server instance.  
		// To start the server listening for connection requests
		// call the Init method followed by Start method 
		//
		// <param name="numConnections">the maximum number of connections the sample is designed to handle simultaneously</param>
		// <param name="receiveBufferSize">buffer size to use for each socket I/O operation</param>
		public Server(int num_connections, int receive_buffer_size) {
			total_bytes_read = 0;
			num_connected_sockets = 0;
			this.num_connections = num_connections;
			this.receive_buffer_size = receive_buffer_size;
			// allocate buffers such that the maximum number of sockets can have one outstanding read and 
			//write posted to the socket simultaneously  
			buffer_manager = new BufferManager(receive_buffer_size * num_connections * OpsToPreAlloc,
				receive_buffer_size);

			read_write_pool = new SocketAsyncEventArgsPool(num_connections);
			max_number_accepted_clients = new Semaphore(num_connections, num_connections);
		}

		// Initializes the server by preallocating reusable buffers and 
		// context objects.  These objects do not need to be preallocated 
		// or reused, but it is done this way to illustrate how the API can 
		// easily be used to create reusable objects to increase server performance.
		//
		public void Init() {
			// Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds 
			// against memory fragmentation
			buffer_manager.InitBuffer();

			// preallocate pool of SocketAsyncEventArgs objects
			SocketAsyncEventArgs read_write_event_arg;

			for (int i = 0; i < num_connections; i++) {
				//Pre-allocate a set of reusable SocketAsyncEventArgs
				read_write_event_arg = new SocketAsyncEventArgs();
				read_write_event_arg.Completed += IO_Completed;
				read_write_event_arg.UserToken = new AsyncUserToken();

				// assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
				buffer_manager.SetBuffer(read_write_event_arg);

				// add SocketAsyncEventArg to the pool
				read_write_pool.Push(read_write_event_arg);
			}

		}

		// Starts the server such that it is listening for 
		// incoming connection requests.    
		//
		// <param name="localEndPoint">The endpoint which the server will listening 
		// for connection requests on</param>
		public void Start(IPEndPoint local_end_point) {
			// create the socket which listens for incoming connections
			listen_socket = new Socket(local_end_point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			listen_socket.Bind(local_end_point);
			// start the server with a listen backlog of 100 connections
			listen_socket.Listen(100);

			// post accepts on the listening socket
			StartAccept(null);

			//Console.WriteLine("{0} connected sockets with one outstanding receive posted to each....press any key", m_outstandingReadCount);
			Console.WriteLine("Press any key to terminate the server process....");
			Console.ReadKey();
		}


		// Begins an operation to accept a connection request from the client 
		//
		// <param name="acceptEventArg">The context object to use when issuing 
		// the accept operation on the server's listening socket</param>
		public void StartAccept(SocketAsyncEventArgs accept_event_arg) {
			if (accept_event_arg == null) {
				accept_event_arg = new SocketAsyncEventArgs();
				accept_event_arg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
			} else {
				// socket must be cleared since the context object is being reused
				accept_event_arg.AcceptSocket = null;
			}

			max_number_accepted_clients.WaitOne();
			bool will_raise_event = listen_socket.AcceptAsync(accept_event_arg);
			if (!will_raise_event) {
				ProcessAccept(accept_event_arg);
			}
		}

		// This method is the callback method associated with Socket.AcceptAsync 
		// operations and is invoked when an accept operation is complete
		//
		void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e) {
			ProcessAccept(e);
		}

		private void ProcessAccept(SocketAsyncEventArgs e) {
			Interlocked.Increment(ref num_connected_sockets);
			Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
				num_connected_sockets);

			// Get the socket for the accepted client connection and put it into the 
			//ReadEventArg object user token
			SocketAsyncEventArgs read_event_args = read_write_pool.Pop();
			((AsyncUserToken)read_event_args.UserToken).Socket = e.AcceptSocket;

			// As soon as the client is connected, post a receive to the connection
			bool will_raise_event = e.AcceptSocket.ReceiveAsync(read_event_args);
			if (!will_raise_event) {
				ProcessReceive(read_event_args);
			}

			// Accept the next connection request
			StartAccept(e);
		}

		// This method is called whenever a receive or send operation is completed on a socket 
		//
		// <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
		void IO_Completed(object sender, SocketAsyncEventArgs e) {
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

		// This method is invoked when an asynchronous receive operation completes. 
		// If the remote host closed the connection, then the socket is closed.  
		// If data was received then the data is echoed back to the client.
		//
		private void ProcessReceive(SocketAsyncEventArgs e) {
			// check if the remote host closed the connection
			AsyncUserToken token = (AsyncUserToken)e.UserToken;
			if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success) {
				//increment the count of the total bytes receive by the server
				Interlocked.Add(ref total_bytes_read, e.BytesTransferred);
				Console.WriteLine("The server has read a total of {0} bytes", total_bytes_read);

				//echo the data received back to the client
				e.SetBuffer(e.Offset, e.BytesTransferred);
				bool will_raise_event = token.Socket.SendAsync(e);
				if (!will_raise_event) {
					ProcessSend(e);
				}

			} else {
				CloseClientSocket(e);
			}
		}

		// This method is invoked when an asynchronous send operation completes.  
		// The method issues another receive on the socket to read any additional 
		// data sent from the client
		//
		// <param name="e"></param>
		private void ProcessSend(SocketAsyncEventArgs e) {
			if (e.SocketError == SocketError.Success) {
				// done echoing data back to the client
				AsyncUserToken token = (AsyncUserToken)e.UserToken;
				// read the next block of data send from the client
				bool will_raise_event = token.Socket.ReceiveAsync(e);
				if (!will_raise_event) {
					ProcessReceive(e);
				}
			} else {
				CloseClientSocket(e);
			}
		}

		private void CloseClientSocket(SocketAsyncEventArgs e) {
			AsyncUserToken token = e.UserToken as AsyncUserToken;

			// close the socket associated with the client
			try {
				token.Socket.Shutdown(SocketShutdown.Send);
			}
			// throws if client process has already closed
			catch (Exception) { }
			token.Socket.Close();

			// decrement the counter keeping track of the total number of clients connected to the server
			Interlocked.Decrement(ref num_connected_sockets);
			max_number_accepted_clients.Release();
			Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", num_connected_sockets);

			// Free the SocketAsyncEventArg so they can be reused by another client
			read_write_pool.Push(e);
		}

		internal class AsyncUserToken {
			public Socket Socket { get; set; }
		}

	}

	
}
