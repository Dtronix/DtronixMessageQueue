using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TransportLayer.Tcp
{
    public class TcpTransportLayerSession : ITransportLayerSession
    {
        public Socket Socket { get; }

        public Guid Id { get; }


        public object ImplementedSession { get; set; }

        public TransportLayerState State { get; private set; }

        public event EventHandler<TransportLayerSessionCloseEventArgs> Closing;
        public event EventHandler<TransportLayerSessionCloseEventArgs> Closed;
        public event EventHandler<byte[]> Received;

        public TcpTransportLayer TransportLayer { get; }

        /// <summary>
        /// Async args used to send data to the wire.
        /// </summary>
        private SocketAsyncEventArgs _sendArgs;

        /// <summary>
        /// Async args used to receive data off the wire.
        /// </summary>
        private SocketAsyncEventArgs _receiveArgs;

        //private SemaphoreSlim _writeSemaphore;

        /// <summary>
        /// Last time the session received anything from the socket.
        /// </summary>
        public DateTime LastReceived { get; private set; }

        /// <summary>
        /// Time that this session connected to the server.
        /// </summary>
        public DateTime ConnectedTime { get; private set; }


        public TcpTransportLayerSession(TcpTransportLayer transportLayer, Socket socket)
        {
            Socket = socket;
            TransportLayer = transportLayer;

            _sendArgs = transportLayer.AsyncManager.Create();
            _receiveArgs = transportLayer.AsyncManager.Create();

            // TODO: Review if this is necessary due to the new ActionProcessor.
            //_writeSemaphore = new SemaphoreSlim(1, 1);

            Id = Guid.NewGuid();

            if (transportLayer.Config.SendTimeout > 0)
               socket.SendTimeout = transportLayer.Config.SendTimeout;

            if (transportLayer.Config.SendAndReceiveBufferSize > 0)
                socket.ReceiveBufferSize = transportLayer.Config.SendAndReceiveBufferSize;

            if (transportLayer.Config.SendAndReceiveBufferSize > 0)
                socket.SendBufferSize = transportLayer.Config.SendAndReceiveBufferSize;

            socket.NoDelay = true;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
        }


        /// <summary>
        /// This method is called whenever a receive or send operation is completed on a socket 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
        protected virtual void IoCompleted(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    LastReceived = DateTime.Now;
                    ReceiveComplete(e);
                    break;

                case SocketAsyncOperation.Send:
                    SendComplete(e);
                    break;

                default:
                    throw new ArgumentException(
                        "The last operation completed on the socket was not a receive, send connect or disconnect.");
            }
        }




        /// <summary>
        /// Sends raw bytes to the socket.  Blocks until data is sent.
        /// </summary>
        /// <param name="buffer">Buffer bytes to send.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="length">Total bytes to send.</param>
        public virtual void Send(byte[] buffer, int offset, int length)
        {
            if (Socket == null || Socket.Connected == false)
                return;

            //_writeSemaphore.Wait(-1);

            // Copy the bytes to the block buffer
            Buffer.BlockCopy(buffer, offset, _sendArgs.Buffer, _sendArgs.Offset, length);

            // Update the buffer length.
            _sendArgs.SetBuffer(_sendArgs.Offset, length);

            try
            {
                if (Socket.SendAsync(_sendArgs) == false)
                {
                    IoCompleted(this, _sendArgs);
                }
            }
            catch (ObjectDisposedException)
            {
                Close(SessionCloseReason.SocketError);
            }
        }

        public void Receive()
        {
            try
            {
                // Re-setup the receive async call.
                if (Socket.ReceiveAsync(_receiveArgs) == false)
                {
                    IoCompleted(this, _receiveArgs);
                }
            }
            catch (ObjectDisposedException)
            {
                Close(SessionCloseReason.SocketError);
            }
            
        }


        /// <summary>
        /// This method is invoked when an asynchronous receive operation completes. 
        /// If the remote host closed the connection, then the socket is closed.
        /// </summary>
        /// <param name="e">Event args of this action.</param>
        protected void ReceiveComplete(SocketAsyncEventArgs e)
        {
            if (State == TransportLayerState.Closing)
                return;

            if (e.BytesTransferred == 0 && State == TransportLayerState.Connected)
            {
                State = TransportLayerState.Closing;
                return;
            }
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                // Update the last time this session was active to prevent timeout.
                LastReceived = DateTime.UtcNow;

                // Create a copy of these bytes.
                var buffer = new byte[e.BytesTransferred];

                Buffer.BlockCopy(e.Buffer, e.Offset, buffer, 0, e.BytesTransferred);

                Received?.Invoke(this, buffer);
            }
            else
            {
                Close(SessionCloseReason.SocketError);
            }
        }

        public void Close(SessionCloseReason reason)
        {
            // If this session has already been closed, nothing more to do.
            if (State == TransportLayerState.Closed)
                return;

            Closing?.Invoke(this, new TransportLayerSessionCloseEventArgs(this, reason));

            // Set the state to closing to restrict what can be done.
            State = TransportLayerState.Closing;

            // Prevent any more bytes from being received.
            Received = null;

            // close the socket associated with the client
            try
            {
                Socket.Shutdown(SocketShutdown.Receive);
                Socket.Disconnect(false);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                Socket.Close(1000);
            }

            _sendArgs.Completed -= IoCompleted;
            _receiveArgs.Completed -= IoCompleted;

            // Free the SocketAsyncEventArg so they can be reused by another client
            TransportLayer.AsyncManager.Free(_sendArgs);
            TransportLayer.AsyncManager.Free(_receiveArgs);

            State = TransportLayerState.Closed;

            Closed?.Invoke(this, new TransportLayerSessionCloseEventArgs(this, reason));
        }


        /// <summary>
        /// This method is invoked when an asynchronous send operation completes.  
        /// The method issues another receive on the socket to read any additional data sent from the client
        /// </summary>
        /// <param name="e">Event args of this action.</param>
        private void SendComplete(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Close(SessionCloseReason.SocketError);
            }
            //_writeSemaphore.Release(1);
        }
    }
}
