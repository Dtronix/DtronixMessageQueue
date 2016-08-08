using System;
using System.Diagnostics;
using System.Threading;
using DtronixMessageQueue;

namespace MqPerformanceTests {
	class Program {
		static void Main(string[] args) {
			MqPerformanceTests();
		}

		private static void MqPerformanceTests() {
			var server = new MqServer();
			server.Start();

			int runs = 1000000;
			int count = 0;
			Stopwatch sw = new Stopwatch();
			var wait = new AutoResetEvent(false);

			var client = new MqClient();

			server.IncomingMessage += (sender, args2) => {
				MqMessage message_out;

				while (args2.Mailbox.Inbox.TryDequeue(out message_out)) {
					count++;
				}

				if (count == runs) {
					sw.Stop();
					Console.WriteLine($"Sent {runs} messages in {sw.ElapsedMilliseconds}ms. {(int)((double)runs / sw.ElapsedMilliseconds * 1000)} Messages per second.");
					wait.Set();
				}
			};


			var message = new MqMessage {
					new MqFrame(RandomBytes(50), MqFrameType.More),
					new MqFrame(RandomBytes(50), MqFrameType.More),
					new MqFrame(RandomBytes(50), MqFrameType.More),
					new MqFrame(RandomBytes(50), MqFrameType.Last)
				};


			var send = new Action(() => {
				Console.WriteLine("Running...");
				count = 0;
				sw.Restart();
				for (int i = 0; i < runs; i++) {
					client.Send(message);
				}
				Console.ReadLine();
			});

			client.Connected += (sender, args2) => {
				//send();
			};

			client.ConnectAsync("127.0.0.1").Wait();

			while (true) {
				send();
			}
		}


		private static byte[] RandomBytes(int len) {
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
