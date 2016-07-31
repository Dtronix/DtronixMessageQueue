using System;
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
			server.Start();

			Thread.Sleep(300);

			var client = new MQClient();

			var message = new MQMessage {
				new MQFrame(new byte[] {1, 2, 3, 4}, MQFrameType.More),
				new MQFrame(new byte[] {0, 9, 8, 7}, MQFrameType.More),
				new MQFrame(new byte[] {1, 0, 2, 9}, MQFrameType.Last)
			};

			client.Connect("127.0.0.1");
			Thread.Sleep(300);

			for (int i = 0; i < 10000; i++) {
				client.Send(message);
			}
			


			client.Dispose();

			Thread.Sleep(10000);


		}
	}
}
