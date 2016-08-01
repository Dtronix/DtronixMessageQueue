using System;
using System.Diagnostics;
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

			int runs = 100;
			Stopwatch sw = new Stopwatch();
			var wait = new AutoResetEvent(false);

			var client = new MQClient();
			server.OnIncomingMessage += (sender, args) => {
				if (args.Mailbox.Count == runs) {
					sw.Stop();
					output.WriteLine($"Used {client.WritePool.Count} event args to write.");
					output.WriteLine($"Sent {runs} messages in {sw.ElapsedMilliseconds}");
					wait.Set();
				}
			};

			Thread.Sleep(300);



			var message = new MQMessage {
				new MQFrame(new byte[] {1, 2, 3, 4}, MQFrameType.More),
				new MQFrame(new byte[] {0, 9, 8, 7}, MQFrameType.More),
				new MQFrame(new byte[] {1, 0, 2, 9}, MQFrameType.Last)
			};

			var message2 = new MQMessage {
					new MQFrame(RandomBytes(100), MQFrameType.More),
				new MQFrame(RandomBytes(100), MQFrameType.More),
					new MQFrame(RandomBytes(100), MQFrameType.More),
				new MQFrame(RandomBytes(120), MQFrameType.Last)
			};

			client.Connect("127.0.0.1");
			Thread.Sleep(300);
			sw.Start();

			for (int i = 0; i < runs; i++) {
				client.Send(message2);
			}

			

			wait.WaitOne(10000);


			client.Dispose();
			server.Dispose();


		}


		private byte[] RandomBytes(int len) {
			byte[] val = new byte[len];
			Random rand = new Random();
			rand.NextBytes(val);

			return val;
		}
	}
}
