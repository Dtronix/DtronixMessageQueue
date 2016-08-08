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
			bool running = false;

			var client = new MqClient();

			Console.WriteLine("| Build Type   | Messages     | Miliseconds  | MPS        | MBps     |");
			Console.WriteLine("|--------------|--------------|--------------|------------|----------|");

			var message = new MqMessage {
					new MqFrame(RandomBytes(50), MqFrameType.More),
					new MqFrame(RandomBytes(50), MqFrameType.More),
					new MqFrame(RandomBytes(50), MqFrameType.More),
					new MqFrame(RandomBytes(50), MqFrameType.Last)
				};

			var message_size = message.Size;

			server.IncomingMessage += (sender, args2) => {
				MqMessage message_out;

				while (args2.Mailbox.Inbox.TryDequeue(out message_out)) {
					count++;
				}

				if (count == runs) {
					sw.Stop();
					string mode = "Release";

#if DEBUG
					mode = "Debug";
#endif

					var messages_per_second = (int)((double)runs / sw.ElapsedMilliseconds * 1000);
					var mbps = runs * (double)(message_size - 12) / sw.ElapsedMilliseconds / 1000;
					Console.CursorLeft = 0;
					Console.WriteLine("| {0,12} | {1,12:N0} | {2,12:N0} | {3,10:N0} | {4,8:N2} |", mode, runs, sw.ElapsedMilliseconds, messages_per_second, mbps);
					running = false;
				}

			};



			var send = new Action(() => {
				if (running) {
					return;
				}
				running = true;
				Console.WriteLine("Running...");
				Console.CursorTop -= 1;
				count = 0;
				sw.Restart();
				for (int i = 0; i < runs; i++) {
					client.Send(message);
				}
				Console.ReadLine();
				Console.CursorTop -= 1;

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
