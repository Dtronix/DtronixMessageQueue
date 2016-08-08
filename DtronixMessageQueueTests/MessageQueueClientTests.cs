using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueueTests {

	public class MessageQueueClientTests : MessageQueueTestsBase {

		public MessageQueueClientTests(ITestOutputHelper output) : base(output) {

		}

		[Theory]
		[InlineData(1, false)]
		[InlineData(1, true)]
		[InlineData(50, true)]
		[InlineData(1000, true)]
		public void Client_should_send_data_to_server(int number, bool validate) {
			var message_source = GenerateRandomMessage(4, 50);
			int received_messages = 0;
			Client.Connected += (sender, args) => {
				for (int i = 0; i < number; i++) {
					Client.Send(message_source);
				}
			};

			Server.IncomingMessage += (sender, args) => {
				MqMessage message;

				while (args.Mailbox.Inbox.TryDequeue(out message)) {
					received_messages++;
					if (validate) {
						CompareMessages(message_source, message);
					}
				}


				if (received_messages == number) {
					TestStatus.Set();
				}
			};

			StartAndWait();
		}



		[Fact]
		public void Client_does_not_send_empty_message() {
			var message_source = GenerateRandomMessage(4, 50);
			int received_messages = 0;
			Client.Connected += (sender, args) => {
				Client.Send(new MqMessage());
				Client.Send(message_source);
			};

			Server.IncomingMessage += (sender, args) => {
				MqMessage message;

				while (args.Mailbox.Inbox.TryDequeue(out message)) {
					received_messages++;
				}

				if (received_messages == 1) {
					TestStatus.Set();
				}
			};

			StartAndWait();
		}


		[Fact]
		public void Client_connects_to_server() {

			Client.Connected += (sender, args) => TestStatus.Set();

			StartAndWait();
		}


		[Fact]
		public void Client_disconects_from_server() {

			Client.Connected += async (sender, args) => {
				await Client.Close();
			};

			Client.Closed += (sender, args) => TestStatus.Set();

			StartAndWait();
		}

		[Fact]
		public void Client_notified_server_stopping() {

			Server.NewSessionConnected += session => Server.Stop();

			Client.Closed += (sender, args) => TestStatus.Set();

			StartAndWait();
		}

		[Fact]
		public void Client_closes_self() {

			Client.Connected += (sender, args) => Client.Close();

			Client.Closed += (sender, args) => TestStatus.Set();

			StartAndWait();
		}

		[Fact]
		public void Client_notified_server_session_closed() {

			Server.NewSessionConnected += session => session.Close(CloseReason.Unknown);

			Client.Closed += (sender, args) => TestStatus.Set();

			StartAndWait();
		}

	}
}
