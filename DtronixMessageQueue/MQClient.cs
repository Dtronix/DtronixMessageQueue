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
	public class MqClient : EasyClientBase, IDisposable {
		private MqPostmaster postmaster;

		private readonly MqMailbox mailbox;


		public event EventHandler<IncomingMessageEventArgs> IncomingMessage;


		public int MaxRequestLength { get; set; } = 1024*16;

		public MqClient() {
			PipeLineProcessor = new DefaultPipelineProcessor<BufferedPackageInfo>(new MqClientReceiveFilter(), MaxRequestLength);

			postmaster = new MqPostmaster(MaxRequestLength);

			mailbox = new MqMailbox(postmaster, this);

			mailbox.IncomingMessage += OnIncomingMessage;
		}

		private void OnIncomingMessage(object sender, IncomingMessageEventArgs e) {
			IncomingMessage?.Invoke(sender, e);
		}

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

			mailbox.EnqueueIncomingBuffer(buffer);
		}

		public Task ConnectAsync(string address, int port = 2828) {
			return ConnectAsync(new IPEndPoint(IPAddress.Parse(address), port));
		}

		public void Send(MqMessage message) {
			if (IsConnected == false) {
				throw new InvalidOperationException("Can not send messages while disconnected from server.");
			}

			mailbox.EnqueueOutgoingMessage(message);
		}

		public void Dispose() {
			mailbox.IncomingMessage -= OnIncomingMessage;
			Close();
			postmaster.Dispose();
			mailbox.Dispose();
		}
	}
}