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

			var server = new MqServer(new MqServer.Config());
			server.Start(new IPEndPoint(IPAddress.Any, 2828));

			int runs = 1000;
			Stopwatch sw = new Stopwatch();
			var wait = new AutoResetEvent(false);

			var client = new MqClient();

			server.InboxMessage += (sender, args) => {
				if (args.Mailbox.Inbox.Count == runs) {
					sw.Stop();
					output.WriteLine($"Used {args.Mailbox.Inbox.Count} event args to write.");
					output.WriteLine($"Sent {runs} messages in {sw.ElapsedMilliseconds}. {(double)runs/ sw.ElapsedMilliseconds*1000}");
					wait.Set();
				}
			};

			Thread.Sleep(300);



			var message = new MqMessage {
				new MqFrame(new byte[] {1, 2, 3, 4}, MqFrameType.More),
				new MqFrame(new byte[] {0, 9, 8, 7}, MqFrameType.More),
				new MqFrame(new byte[] {1, 0, 2, 9}, MqFrameType.Last)
			};

			var message2 = new MqMessage {
					new MqFrame(RandomBytes(15), MqFrameType.More),
				new MqFrame(RandomBytes(30), MqFrameType.More),
					new MqFrame(RandomBytes(72), MqFrameType.More),
				new MqFrame(RandomBytes(86), MqFrameType.Last)
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
