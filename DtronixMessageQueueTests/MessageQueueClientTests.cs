using System;
using System.Net;
using System.Threading;
using DtronixMessageQueue;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueueTests {

	public class MessageQueueClientTests {
		private ITestOutputHelper output;

		public MessageQueueClientTests(ITestOutputHelper output) {
			this.output = output;
		}

		[Fact]
		public void Client_sends_data_to_server() {

			var server = new MQServer(new MQServer.Config());
			server.Start(new IPEndPoint(IPAddress.Any, 2828));

			server.OnIncomingMessage += (sender, args) => {
				var mb = args.Mailbox;
			};

			Thread.Sleep(300);

			var client = new MQClient();

			var message = new MQMessage {
				new MQFrame(new byte[] {1, 2, 3, 4}, MQFrameType.More),
				new MQFrame(new byte[] {0, 9, 8, 7}, MQFrameType.More),
				new MQFrame(new byte[] {1, 0, 2, 9}, MQFrameType.Last)
			};

			var message2 = new MQMessage {
				new MQFrame(new byte[] {1, 2, 3, 4}, MQFrameType.Last)
			};

			client.Connect("127.0.0.1");
			Thread.Sleep(300);

			for (int i = 0; i < 1; i++) {
				client.Send(message);
				client.Send(message2);
			}
			


			client.Dispose();

			Thread.Sleep(10000);


		}
	}
}
