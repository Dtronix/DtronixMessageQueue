using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using DtronixMessageQueue;
using SuperSocket.SocketBase.Config;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueueTests {

	public class MessageQueueClientTests : MessageQueueTestsBase {

		public MessageQueueClientTests(ITestOutputHelper output) : base(output) {
			
		}

		[Fact]
		public void Client_sends_message_to_server() {
			var message_source = GenerateRandomMessage();

			Server.IncomingMessage += (sender, args) => {
				MqMessage message;
				args.Mailbox.Inbox.TryDequeue(out message);

				CompareMessages(message_source, message);

				TestStatus.Set();
			};

			Client.Connected += (sender, args) => {
				Client.Send(message_source);
			};

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
		public void Client_connects_to_server() {

			Client.Connected += (sender, args) => TestStatus.Set();

			StartAndWait();
		}

		[Fact]
		public void Client_notified_server_stopping() {

			Client.Connected += (sender, args) => Server.Stop();

			Client.Closed += (sender, args) => TestStatus.Set();

			

			StartAndWait();
		}

	}
}
