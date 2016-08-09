using System;
using System.Diagnostics;
using System.Threading;
using DtronixMessageQueue;

namespace DtronixMessageQueue.Tests.Performance {
	class Program {
		static void Main(string[] args) {

			var small_message = new MqMessage {
				new MqFrame(RandomBytes(50), MqFrameType.More),
				new MqFrame(RandomBytes(50), MqFrameType.More),
				new MqFrame(RandomBytes(50), MqFrameType.More),
				new MqFrame(RandomBytes(50), MqFrameType.Last)
			};

			MqPerformanceTests(1000000, 5, small_message);

			var medimum_message = new MqMessage {
				new MqFrame(RandomBytes(500), MqFrameType.More),
				new MqFrame(RandomBytes(500), MqFrameType.More),
				new MqFrame(RandomBytes(500), MqFrameType.More),
				new MqFrame(RandomBytes(500), MqFrameType.Last)
			};

			MqPerformanceTests(100000, 5, medimum_message);

			var large_message = new MqMessage {
				new MqFrame(RandomBytes(15000), MqFrameType.More),
				new MqFrame(RandomBytes(15000), MqFrameType.More),
				new MqFrame(RandomBytes(15000), MqFrameType.More),
				new MqFrame(RandomBytes(15000), MqFrameType.Last)
			};

			MqPerformanceTests(10000, 5, large_message);

			Console.WriteLine("Performance complete");
		}

		private static void MqPerformanceTests(int runs, int loops, MqMessage message) {
			var server = new MqServer();
			server.Start();

			double[] total_values = { 0, 0, 0 };

			var count = 0;
			var sw = new Stopwatch();
			var wait = new AutoResetEvent(false);

			var client = new MqClient();

			Console.WriteLine("|   Build |   Messages | Msg Bytes | Milliseconds |        MPS |     MBps |");
			Console.WriteLine("|---------|------------|-----------|--------------|------------|----------|");


			var message_size = message.Size;

			server.IncomingMessage += (sender, args2) => {
				MqMessage message_out;

				while (args2.Mailbox.Inbox.TryDequeue(out message_out)) {
					count++;
				}

				if (count == runs) {
					sw.Stop();
					var mode = "Release";

#if DEBUG
					mode = "Debug";
#endif

					var messages_per_second = (int)((double)runs / sw.ElapsedMilliseconds * 1000);
					var msg_size_no_header = message_size - 12;
					var mbps = runs * (double)(msg_size_no_header) / sw.ElapsedMilliseconds / 1000;
					Console.WriteLine("| {0,7} | {1,10:N0} | {2,9:N0} | {3,12:N0} | {4,10:N0} | {5,8:N2} |", mode, runs, msg_size_no_header, sw.ElapsedMilliseconds, messages_per_second, mbps);
					total_values[0] += sw.ElapsedMilliseconds;
					total_values[1] += messages_per_second;
					total_values[2] += mbps;


					wait.Set();
				}

			};



			var send = new Action(() => {
				count = 0;
				sw.Restart();
				for (var i = 0; i < runs; i++) {
					client.Send(message);
				}
				wait.WaitOne();
				wait.Reset();

			});

			client.ConnectAsync("::1").Wait();

			for (var i = 0; i < loops; i++) {
				send();
			}

			Console.WriteLine("|         |            |  AVERAGES | {0,12:N0} | {1,10:N0} | {2,8:N2} |", total_values[0] / loops, total_values[1] / loops, total_values[2] / loops);
			Console.WriteLine();

			server.Stop();
			client.Close().Wait();

		}


		private static byte[] RandomBytes(int len) {
			var number = 0;
			var val = new byte[len];

			for (var i = 0; i < len; i++) {
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
