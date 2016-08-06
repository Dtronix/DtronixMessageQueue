using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ClientEngine;
using SuperSocket.ProtoBase;

namespace DtronixMessageQueue {


	public class MqClient : EasyClientBase {

		public MqPostmaster Postmaster { get; }

		private readonly MqMailbox mailbox;

		public int MaxRequestLength { get; set; } = 1024*16;

		public MqClient() {
			PipeLineProcessor = new DefaultPipelineProcessor<BufferedPackageInfo>(new MqClientReceiveFilter(), MaxRequestLength);

			Postmaster = new MqPostmaster(MaxRequestLength);

			mailbox = new MqMailbox(Postmaster, this);
		}

		protected override void HandlePackage(IPackageInfo package) {
			var buff_package = package as BufferedPackageInfo;

			if (buff_package == null) {
				return;
			}

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
	}
}
