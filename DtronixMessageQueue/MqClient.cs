using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ClientEngine;
using SuperSocket.Common;
using SuperSocket.ProtoBase;

namespace DtronixMessageQueue {

	/// <summary>
	/// Client used to connect to a remote message queue server.
	/// </summary>
	public class MqClient : EasyClientBase, IDisposable {

		/// <summary>
		/// Internal postmaster.
		/// </summary>
		private readonly MqPostmaster postmaster;

		/// <summary>
		/// Mailbox for this client.
		/// </summary>
		private readonly MqMailbox mailbox;

		/// <summary>
		/// Represents the maximum size in bytes that a frame can be. (including headers)
		/// </summary>
		//public int MaxRequestLength { get; set; } = 1024 * 16;

		/// <summary>
		/// Event fired when a new message arrives at the mailbox.
		/// </summary>
		public event EventHandler<IncomingMessageEventArgs> IncomingMessage;


		/// <summary>
		/// Initializes a new instance of a message queue.
		/// </summary>
		public MqClient() {
			PipeLineProcessor = new DefaultPipelineProcessor<BufferedPackageInfo>(new MqClientReceiveFilter(), MqFrame.MaxFrameSize);

			postmaster = new MqPostmaster();

			mailbox = new MqMailbox(postmaster, this);

			mailbox.IncomingMessage += OnIncomingMessage;
		}

		/// <summary>
		/// Event method invoker
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The event object containing the mailbox to retrieve the message from.</param>
		private void OnIncomingMessage(object sender, IncomingMessageEventArgs e) {
			IncomingMessage?.Invoke(sender, e);
		}

		/// <summary>
		/// Internal method to retrieve all the bytes on the wire and enqueue them to be processed by a separate thread.
		/// </summary>
		/// <param name="package">Package to process</param>
		protected override void HandlePackage(IPackageInfo package) {
			var buff_package = package as BufferedPackageInfo;

			if (buff_package == null) {
				return;
			}

			var data_list = (BufferList) buff_package.Data;

			byte[] buffer = new byte[data_list.Total - 3];
			int writer_offset = 0;

			foreach (var buffer_seg in data_list) {
				// If the first array is the exact length of our header, ignore it.
				if (writer_offset == 0 && buffer_seg.Count == 3) {
					continue;
				}
				//TODO: Verify whether or not the "Package" already copies the data or if it is from a large buffer source.
				// Offset the destination -3 due to the offset containing the header.
				Buffer.BlockCopy(buffer_seg.Array, buffer_seg.Offset, buffer, writer_offset, buffer_seg.Count);
				writer_offset += buffer_seg.Count;
			}

			// Enqueue the incoming message to be processed by the postmaster.
			mailbox.EnqueueIncomingBuffer(buffer);
		}

		/// <summary>
		/// Connects to a serve endpoint.
		/// </summary>
		/// <param name="address">Server address to connect to.</param>
		/// <param name="port">Server port to connect to.  Default is 2828.</param>
		/// <returns>Awaitable task.</returns>
		public Task ConnectAsync(string address, int port = 2828) {
			return ConnectAsync(new IPEndPoint(IPAddress.Parse(address), port));
		}

		/// <summary>
		/// Adds a message to the outbox to be processed.
		/// Empty messages will be ignored.
		/// </summary>
		/// <param name="message">Message to send.</param>
		public void Send(MqMessage message) {
			if (IsConnected == false) {
				throw new InvalidOperationException("Can not send messages while disconnected from server.");
			}

			if (message.Count == 0) {
				return;
			}

			// Enqueue the outgoing message to be processed by the postmaster.
			mailbox.EnqueueOutgoingMessage(message);
		}

		/// <summary>
		/// Disposes of all resources associated with this client.
		/// </summary>
		public void Dispose() {
			mailbox.IncomingMessage -= OnIncomingMessage;
			Close();
			postmaster.Dispose();
			mailbox.Dispose();
		}
	}
}