using System;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests {


	public class MqServerTests : MqTestsBase {

		public MqServerTests(ITestOutputHelper output) : base(output) {

		}


		[Theory]
		[InlineData(1, false)]
		[InlineData(1, true)]
		public void Server_should_send_data_to_client(int number, bool validate) {
			var message_source = GenerateRandomMessage(4, 50);

			Server.Connected += (sender, session) => {
				for (int i = 0; i < number; i++) {
					session.Session.Send(message_source);
				}
				
			};

			int client_message_count = 0;
			Client.IncomingMessage += (sender, args) => {
				MqMessage message;

				client_message_count += args.Messages.Count;

				while (args.Messages.Count > 0) {
					message = args.Messages.Dequeue();

					if (validate) {
						CompareMessages(message_source, message);
					}
				}

				if (client_message_count == number) {
					TestStatus.Set();
				}
			};

			StartAndWait();

		}

		[Fact]
		public void Server_accepts_new_connection() {

			Server.Connected += (sender, session) => {
				TestStatus.Set();
			};

			StartAndWait();
		}

		[Fact]
		public void Server_detects_client_disconnect() {

			Client.Connected += (sender, args) => {
				Client.Close();
			};

			Server.Closed += (session, value) => {
				TestStatus.Set();
			};

			StartAndWait();
		}


		[Fact]
		public void Server_stops() {
			Server.Start();
			Assert.Equal(true, Server.IsRunning);
			Server.Stop();
			Assert.Equal(false, Server.IsRunning);
		}
	}
}
