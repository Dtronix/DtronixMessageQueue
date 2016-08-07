using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DtronixMessageQueue;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketEngine.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueueTests {
	public class MessageQueueTestsBase : IDisposable {
		private Random random = new Random();
		public ITestOutputHelper Output;

		public MqServer Server { get; }
		public MqClient Client { get; }
		public int Port { get; }

		public TimeSpan TestTimeout { get; set; } = new TimeSpan(0, 0, 20);

		public ManualResetEventSlim TestStatus { get; set; } = new ManualResetEventSlim(false);

		public MessageQueueTestsBase(ITestOutputHelper output) {
			this.Output = output;

			using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) {
				sock.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0)); // Pass 0 here.
				Port = ((IPEndPoint)sock.LocalEndPoint).Port;
				sock.Close();
			}

			var config = new ServerConfig {
				Ip = "127.0.0.1",
				Port = Port
			};

			Server = new MqServer(config);
			Client = new MqClient();
		}

		public void StartAndWait() {
			Server.Started += async (sender, args) => {
				if (Server.State == ServerState.Running) {
					await Client.ConnectAsync("127.0.0.1", Port);
				} else {
					throw new TimeoutException("Server did not start in a timely manner.");
				}
				
			};
			Server.Start();
			
			TestStatus.Wait(TestTimeout);
		}

		public bool CompareMessages(MqMessage expected, MqMessage actual) {
			// Total frame count comparison.
			Assert.Equal(expected.Count, actual.Count);

			for (int i = 0; i < expected.Count; i++) {
				// Frame length comparison.
				Assert.Equal(expected[0].DataLength, actual[0].DataLength);

				for (int j = 0; j < expected[0].DataLength; j++) {

					// Each byte.
					Assert.Equal(expected[0].Data[j], actual[0].Data[j]);
				}
			}

			return true;
		}

		public MqMessage GenerateRandomMessage() {
			var frame_count = random.Next(8, 16);
			var message = new MqMessage();
			for (int i = 0; i < frame_count; i++) {
				var frame = new MqFrame(SequentialBytes(random.Next(50, 1024 * 16 - 3)), (i + 1 < frame_count)? MqFrameType.More : MqFrameType.Last);
				message.Add(frame);
			}

			return message;

		}

		protected byte[] SequentialBytes(int len) {
			var number = 0;
			byte[] val = new byte[len];

			for (int i = 0; i < len; i++) {
				val[i] = (byte) number++;
				if (number > 255) {
					number = 0;
				}
			}

			return val;
		}


		public void Dispose() {
			Server.Stop();
			Client.Close();
		}
	}
}