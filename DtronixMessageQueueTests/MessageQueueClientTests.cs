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

			int runs = 2000;
			Stopwatch sw = new Stopwatch();
			var wait = new AutoResetEvent(false);

			var client = new MQClient();

			server.InboxMessage += (sender, args) => {
				if (args.Mailbox.Inbox.Count == runs) {
					sw.Stop();
					output.WriteLine($"Used {args.Mailbox.Inbox.Count} event args to write.");
					output.WriteLine($"Sent {runs} messages in {sw.ElapsedMilliseconds}. {(double)runs/ sw.ElapsedMilliseconds*1000}");
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
					new MQFrame(RandomBytes(15), MQFrameType.More),
				new MQFrame(RandomBytes(30), MQFrameType.More),
					new MQFrame(RandomBytes(72), MQFrameType.More),
				new MQFrame(RandomBytes(86), MQFrameType.Last)
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
			var number = 0;
			byte[] val = new byte[len];

			for (int i = 0; i < len; i++) {
				val[i] = (byte)number++;
				if (number > 255) {
					number = 0;
				}
			}

			
			//Random rand = new Random();
			//rand.NextBytes(val);

			return val;
		}
	}
}
