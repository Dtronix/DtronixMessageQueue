using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue;
using SuperSocket.SocketBase.Config;

namespace DtronixMessageQueue.Tests.Performance {
	class Program {

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetPhysicallyInstalledSystemMemory(out long total_memory_in_kilobytes);

		static void Main(string[] args) {
			string mode = null;
			int total_loops, total_messages, total_frames, frame_size, total_clients;

			if (args == null || args.Length == 0) {
				mode = "single-process";
				total_loops = 1;
				total_messages = 1000000;
				total_frames = 4;
				frame_size = 50;
				total_clients = 1;
			} else if (args.Length == 6) {
				mode = args[0];
				total_loops = int.Parse(args[1]);
				total_messages = int.Parse(args[2]);
				total_frames = int.Parse(args[3]);
				frame_size = int.Parse(args[4]);
				total_clients = int.Parse(args[5]);
			} else {
				Console.WriteLine("Invalid parameters passed to performance tester");
				return;
			}

			var exe_path = Assembly.GetExecutingAssembly().Location;

			if (mode == "setup") {
				Process.Start(exe_path, $"server {total_loops} {total_messages} {total_frames} {frame_size} {total_clients}").WaitForExit();
				Thread.Sleep(500);

				for (int i = 0; i < total_clients; i++) {
					Process.Start(exe_path, $"client {total_loops} {total_messages} {total_frames} {frame_size} {total_clients}");
				}
				

			} else if (mode == "client") {
				Console.WriteLine("|   Messages | Msg Bytes | Milliseconds |        MPS |     MBps |");
				Console.WriteLine("|------------|-----------|--------------|------------|----------|");

				StartClient(total_loops, total_messages, total_frames, frame_size);

			} else if (mode == "server") {
				StartServer(total_messages, total_clients);

			}else if (mode == "single-process") {
				Task.Run(() => {
					StartServer(total_messages, total_clients);
				});

				Console.WriteLine("|   Messages | Msg Bytes | Milliseconds |        MPS |     MBps |");
				Console.WriteLine("|------------|-----------|--------------|------------|----------|");

				for (int i = 0; i < total_clients; i++) {
					Task.Run(() => {
						StartClient(total_loops, total_messages, total_frames, frame_size);
					});
				}

			}

			Console.ReadLine();

		}

		private static void StartClient(int total_loops, int total_messages, int total_frames, int frame_size) {
			Console.WriteLine("Client starting...");

			var cl = new MqClient();
			var stopwatch = new Stopwatch();
			var message_reader = new MqMessageReader();
			var message_size = total_frames*frame_size;
			var message = new MqMessage();
			double[] total_values = { 0, 0, 0 };

			ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
			foreach (var o in mos.Get()) {
				var mo = (ManagementObject)o;
				Console.Write(mo["Name"]);
			}


			long mem_kb;
			GetPhysicallyInstalledSystemMemory(out mem_kb);
			Console.WriteLine(" with " + (mem_kb / 1024 / 1024) + " GB of RAM installed.\r\n");


			for (int i = 0; i < total_frames; i++) {
				message.Add(new MqFrame(RandomBytes(frame_size)));
			}

			cl.IncomingMessage += (sender, args) => {
				MqMessage msg;
				if (args.Mailbox.Inbox.TryDequeue(out msg)) {
					message_reader.Message = msg;
					var result = message_reader.ReadString();

					if (result == "COMPLETE") {

						stopwatch.Stop();

						var messages_per_second = (int)((double)total_messages / stopwatch.ElapsedMilliseconds * 1000);
						var msg_size_no_header = message_size;
						var mbps = total_messages * (double)(msg_size_no_header) / stopwatch.ElapsedMilliseconds / 1000;
						Console.WriteLine("| {0,10:N0} | {1,9:N0} | {2,12:N0} | {3,10:N0} | {4,8:N2} |", total_messages, msg_size_no_header, stopwatch.ElapsedMilliseconds, messages_per_second, mbps);

						total_values[0] += stopwatch.ElapsedMilliseconds;
						total_values[1] += messages_per_second;
						total_values[2] += mbps;


						Console.WriteLine("|            |  AVERAGES | {0,12:N0} | {1,10:N0} | {2,8:N2} |", total_values[0] / total_loops, total_values[1] / total_loops, total_values[2] / total_loops);
						Console.WriteLine();
						Console.WriteLine("Test complete");

						cl.Close();
					}else if (result == "START") {
						stopwatch.Start();
						for (var i = 0; i < total_messages; i++) {
							cl.Send(message);
						}
					}
				}
			};

			cl.ConnectAsync("127.0.0.1").Wait();

		}

		private static void StartServer(int total_messages, int total_clients) {
			var builder = new MqMessageBuilder();
			builder.Write("COMPLETE");

			var complete_message = builder.ToMessage(true);

			builder.Write("START");
			var start_message = builder.ToMessage(true);

			Console.WriteLine("Server starting");
			var server = new MqServer(new ServerConfig {
				Ip = "127.0.0.1",
				Port = 2828
			});
			ConcurrentDictionary<MqSession, ClientRunInfo> client_infos = new ConcurrentDictionary<MqSession, ClientRunInfo>();


			server.NewSessionConnected += session => {
				var current_info = new ClientRunInfo() {
					Session = session,
					Runs = 0
				};
				client_infos.TryAdd(session, current_info);

				if (client_infos.Count == total_clients) {

					foreach (var mq_session in client_infos.Keys) {
						mq_session.Send(start_message);
					}
				}
			};

			server.SessionClosed += (session, value) => {
				ClientRunInfo info;
				client_infos.TryRemove(session, out info);
			};

			server.IncomingMessage += (sender, args) => {
				MqMessage message_out;

				var client_info = client_infos[args.Session];
				while (args.Mailbox.Inbox.TryDequeue(out message_out)) {
					client_info.Runs++;
				}

				if (client_info.Runs == total_messages) {
					args.Session.Send(complete_message);
				}

			};


			server.Start();
			Console.WriteLine("Server started.");
		}

		private class ClientRunInfo {
			public int Runs { get; set; }
			public MqSession Session { get; set; }

		}


		private static void MqPerformanceTests(int runs, int loops, MqMessage message) {
			var server = new MqServer(new ServerConfig {
				Ip = "127.0.0.1",
				Port = 2828
			});
			server.Start();

			double[] total_values = { 0, 0, 0 };

			var count = 0;
			var sw = new Stopwatch();
			var wait = new AutoResetEvent(false);


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


			var client = new MqClient();

			var send = new Action(() => {
				count = 0;
				sw.Restart();
				for (var i = 0; i < runs; i++) {
					client.Send(message);
				}
				MqServer sv = server;
				wait.WaitOne();
				wait.Reset();

			});

			client.ConnectAsync("127.0.0.1").Wait();

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
