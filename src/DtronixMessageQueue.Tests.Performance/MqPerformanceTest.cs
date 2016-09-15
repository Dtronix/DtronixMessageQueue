using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Tests.Performance {
	class MqPerformanceTest : PerformanceTestBase {

		public MqPerformanceTest(string[] args) {
			string mode = null;
			int total_loops, total_messages, total_frames, frame_size, total_clients;

			if (args == null || args.Length == 0) {
				mode = "single-process";
				total_loops = 1;
				total_messages = 1000000;
				total_frames = 4;
				frame_size = 50;
				total_clients = 1;
			} else if (args.Length == 7) {
				mode = args[1];
				total_loops = int.Parse(args[2]);
				total_messages = int.Parse(args[3]);
				total_frames = int.Parse(args[4]);
				frame_size = int.Parse(args[5]);
				total_clients = int.Parse(args[6]);
			} else {
				Console.WriteLine("Invalid parameters passed to performance tester");
				return;
			}

			var exe_path = Assembly.GetExecutingAssembly().Location;

			if (mode == "setup") {
				Process.Start(exe_path, $"mq server {total_loops} {total_messages} {total_frames} {frame_size} {total_clients}");

				for (int i = 0; i < total_clients; i++) {
					Process.Start(exe_path, $"mq client {total_loops} {total_messages} {total_frames} {frame_size} {total_clients}");
				}


			} else if (mode == "client") {
				Console.WriteLine("|   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |");
				Console.WriteLine("|------------|-----------|--------------|------------|----------|");

				StartClient(total_loops, total_messages, total_frames, frame_size);

			} else if (mode == "server") {
				StartServer(total_messages, total_clients);

			} else if (mode == "single-process") {
				MqInProcessTest();

			}
		}


		private static void StartClient(int total_loops, int total_messages, int total_frames, int frame_size) {
			var cl = new MqClient<SimpleMqSession, MqConfig>(new MqConfig() {
				Ip = "127.0.0.1",
				Port = 2828
			});

			var stopwatch = new Stopwatch();
			var message_reader = new MqMessageReader();
			var message_size = total_frames*frame_size;
			var message = new MqMessage();
			double[] total_values = {0, 0, 0};

			for (int i = 0; i < total_frames; i++) {
				message.Add(new MqFrame(SequentialBytes(frame_size), MqFrameType.More, (MqConfig)cl.Config));
			}

			cl.IncomingMessage += (sender, args) => {
				MqMessage msg;
				while (args.Messages.Count > 0) {
					msg = args.Messages.Dequeue();

					message_reader.Message = msg;
					var result = message_reader.ReadString();

					if (result == "COMPLETE") {

						if (total_loops-- > 0) {

							stopwatch.Stop();

							var messages_per_second = (int) ((double) total_messages/stopwatch.ElapsedMilliseconds*1000);
							var msg_size_no_header = message_size;
							var mbps = total_messages*(double) (msg_size_no_header)/stopwatch.ElapsedMilliseconds/1000;
							Console.WriteLine("| {0,10:N0} | {1,9:N0} | {2,12:N0} | {3,10:N0} | {4,8:N2} |", total_messages,
								msg_size_no_header, stopwatch.ElapsedMilliseconds, messages_per_second, mbps);

							total_values[0] += stopwatch.ElapsedMilliseconds;
							total_values[1] += messages_per_second;
							total_values[2] += mbps;
						}

						if (total_loops == 0) {

							Console.WriteLine("|            |  AVERAGES | {0,12:N0} | {1,10:N0} | {2,8:N2} |", total_values[0]/total_loops,
								total_values[1]/total_loops, total_values[2]/total_loops);
							Console.WriteLine();
							Console.WriteLine("Test complete");
						}


						cl.Close();
					} else if (result == "START") {
						if (total_loops > 0) {
							stopwatch.Restart();
							for (var i = 0; i < total_messages; i++) {
								cl.Send(message);
							}
						}
					}
				}
			};

			cl.Connect();

		}

		private static void StartServer(int total_messages, int total_clients) {
			var server = new MqServer<SimpleMqSession, MqConfig>(new MqConfig() {
				Ip = "127.0.0.1",
				Port = 2828
			});

			var builder = new MqMessageWriter((MqConfig) server.Config);
			builder.Write("COMPLETE");

			var complete_message = builder.ToMessage(true);

			builder.Write("START");
			var start_message = builder.ToMessage(true);

			ConcurrentDictionary<SimpleMqSession, ClientRunInfo> clients_info =
				new ConcurrentDictionary<SimpleMqSession, ClientRunInfo>();


			server.Connected += (sender, session) => {
				var current_info = new ClientRunInfo() {
					Session = session.Session,
					Runs = 0
				};
				clients_info.TryAdd(session.Session, current_info);

				if (clients_info.Count == total_clients) {

					foreach (var mq_session in clients_info.Keys) {
						mq_session.Send(start_message);
					}
				}
			};

			server.Closed += (session, value) => {
				ClientRunInfo info;
				clients_info.TryRemove(value.Session, out info);
			};

			server.IncomingMessage += (sender, args) => {
				var client_info = clients_info[args.Session];

				// Count the total messages.
				client_info.Runs += args.Messages.Count;

				if (client_info.Runs == total_messages) {
					args.Session.Send(complete_message);
					args.Session.Send(start_message);
					client_info.Runs = 0;
				}

			};


			server.Start();
		}


		static void MqInProcessTest() {
			var config = new MqConfig {
				Ip = "127.0.0.1",
				Port = 2828
			};


			Console.WriteLine("FrameBufferSize: {0}; SendAndReceiveBufferSize: {1}\r\n", config.FrameBufferSize,
				config.SendAndReceiveBufferSize);

			var small_message = new MqMessage {
				new MqFrame(SequentialBytes(50), MqFrameType.More, config),
				new MqFrame(SequentialBytes(50), MqFrameType.More, config),
				new MqFrame(SequentialBytes(50), MqFrameType.More, config),
				new MqFrame(SequentialBytes(50), MqFrameType.Last, config)
			};

			MqInProcessPerformanceTests(1000000, 5, small_message, config);

			var medimum_message = new MqMessage {
				new MqFrame(SequentialBytes(500), MqFrameType.More, config),
				new MqFrame(SequentialBytes(500), MqFrameType.More, config),
				new MqFrame(SequentialBytes(500), MqFrameType.More, config),
				new MqFrame(SequentialBytes(500), MqFrameType.Last, config)
			};

			MqInProcessPerformanceTests(100000, 5, medimum_message, config);

			var large_message = new MqMessage();

			for (int i = 0; i < 20; i++) {
				large_message.Add(new MqFrame(SequentialBytes(3000), MqFrameType.More, config));
			}

			MqInProcessPerformanceTests(10000, 5, large_message, config);

			Console.WriteLine("Performance complete");

			Console.ReadLine();
		}

		private static void MqInProcessPerformanceTests(int runs, int loops, MqMessage message, MqConfig config) {
			var server = new MqServer<SimpleMqSession, MqConfig>(config);
			server.Start();

			double[] total_values = {0, 0, 0};

			var count = 0;
			var sw = new Stopwatch();
			var wait = new AutoResetEvent(false);
			var complete_test = new AutoResetEvent(false);

			var client = new MqClient<SimpleMqSession, MqConfig>(config);

			Console.WriteLine("|   Build |   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |");
			Console.WriteLine("|---------|------------|-----------|--------------|------------|----------|");


			var message_size = message.Size;

			server.IncomingMessage += (sender, args2) => {
				count += args2.Messages.Count;


				if (count == runs) {
					sw.Stop();
					var mode = "Release";

#if DEBUG
					mode = "Debug";
#endif

					var messages_per_second = (int) ((double) runs/sw.ElapsedMilliseconds*1000);
					var msg_size_no_header = message_size - 12;
					var mbps = runs*(double) (msg_size_no_header)/sw.ElapsedMilliseconds/1000;
					Console.WriteLine("| {0,7} | {1,10:N0} | {2,9:N0} | {3,12:N0} | {4,10:N0} | {5,8:N2} |", mode, runs,
						msg_size_no_header, sw.ElapsedMilliseconds, messages_per_second, mbps);
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
				//MqServer sv = server;
				wait.WaitOne();
				wait.Reset();

			});

			client.Connected += (sender, args) => {
				for (var i = 0; i < loops; i++) {
					send();
				}

				Console.WriteLine("|         |            |  AVERAGES | {0,12:N0} | {1,10:N0} | {2,8:N2} |", total_values[0]/loops,
					total_values[1]/loops, total_values[2]/loops);
				Console.WriteLine();

				server.Stop();
				client.Close();
				complete_test.Set();
			};

			client.Connect();

			complete_test.WaitOne();
		}
	}

}
