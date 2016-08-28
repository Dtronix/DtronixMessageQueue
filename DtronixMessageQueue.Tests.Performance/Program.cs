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
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Tests.Performance {
	class Program {

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetPhysicallyInstalledSystemMemory(out long total_memory_in_kilobytes);

		static void Main(string[] args){
			string mode = null;
			int total_loops, total_messages, total_frames, frame_size, total_clients;

			if (args == null || args.Length == 0) {
				mode = "single-process";
				total_loops = 5;
				total_messages = 10000;
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
				WriteSysInfo();
				Console.WriteLine("|   Messages | Msg Bytes | Milliseconds |        MPS |     MBps |");
				Console.WriteLine("|------------|-----------|--------------|------------|----------|");

				StartClient(total_loops, total_messages, total_frames, frame_size);

			} else if (mode == "server") {
				StartServer(total_messages, total_clients);

			}else if (mode == "single-process") {
				Task.Run(() => {
					StartServer(total_messages, total_clients);
				});
				WriteSysInfo();
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

		private static void WriteSysInfo() {
			ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
			foreach (var o in mos.Get()) {
				var mo = (ManagementObject)o;
				Console.Write(mo["Name"]);
			}


			long mem_kb;
			GetPhysicallyInstalledSystemMemory(out mem_kb);
			Console.WriteLine(" with " + (mem_kb / 1024 / 1024) + " GB of RAM installed.\r\n");
		}

		private static void StartClient(int total_loops, int total_messages, int total_frames, int frame_size) {
			var cl = new MqClient(new SocketConfig() {
				Ip = "127.0.0.1",
				Port = 2828
			});

			var stopwatch = new Stopwatch();
			var message_reader = new MqMessageReader();
			var message_size = total_frames*frame_size;
			var message = new MqMessage();
			double[] total_values = { 0, 0, 0 };

			for (int i = 0; i < total_frames; i++) {
				message.Add(new MqFrame(RandomBytes(frame_size)));
			}

			cl.IncomingMessage += (sender, args) => {
				MqMessage msg;
				while(args.Mailbox.Inbox.TryDequeue(out msg)) {
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
					}else if (result == "START") {
						stopwatch.Restart();
						for (var i = 0; i < total_messages; i++) {
							cl.Send(message);
						}

						Thread.Sleep(500);
					}
				}
			};

			cl.Connect();

		}

		private static void StartServer(int total_messages, int total_clients) {
			var builder = new MqMessageWriter();
			builder.Write("COMPLETE");

			var complete_message = builder.ToMessage(true);

			builder.Write("START");
			var start_message = builder.ToMessage(true);

			var server = new MqServer(new SocketConfig() {
				Ip = "127.0.0.1",
				Port = 2828
			});
			ConcurrentDictionary<MqSession, ClientRunInfo> client_infos = new ConcurrentDictionary<MqSession, ClientRunInfo>();


			server.Connected += (sender, session) => {
				var current_info = new ClientRunInfo() {
					Session = session.Session,
					Runs = 0
				};
				client_infos.TryAdd(session.Session, current_info);

				if (client_infos.Count == total_clients) {

					foreach (var mq_session in client_infos.Keys) {
						mq_session.Send(start_message);
					}
				}
			};

			server.Closed += (session, value) => {
				ClientRunInfo info;
				client_infos.TryRemove(value.Session, out info);
			};

			server.IncomingMessage += (sender, args) => {
				MqMessage message_out;

				var client_info = client_infos[args.Session];
				while (args.Mailbox.Inbox.TryDequeue(out message_out)) {
					client_info.Runs++;
				}

				if (client_info.Runs == total_messages) {
					args.Session.Send(complete_message);
					args.Session.Send(start_message);
					client_info.Runs = 0;
				}

			};


			server.Start();
		}

		private class ClientRunInfo {
			public int Runs { get; set; }
			public MqSession Session { get; set; }

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
