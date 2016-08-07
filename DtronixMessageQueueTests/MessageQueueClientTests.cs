using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using DtronixMessageQueue;
using SuperSocket.SocketBase;
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

			Client.Connected += (sender, args) => {
				Client.Send(message_source);
			};

			Server.IncomingMessage += (sender, args) => {
				MqMessage message;
				args.Mailbox.Inbox.TryDequeue(out message);
				TestStatus.Set();
			};

			StartAndWait();
		}

		[Fact]
		public void Client_sends_valid_message_to_server() {
			var message_source = GenerateRandomMessage();

			Client.Connected += (sender, args) => {
				Client.Send(message_source);
			};

			Server.IncomingMessage += (sender, args) => {
				MqMessage message;
				args.Mailbox.Inbox.TryDequeue(out message);

				CompareMessages(message_source, message);

				TestStatus.Set();
			};

			StartAndWait();
		}

		[Fact]
		public void Client_receives_message_from_server() {
			var message_source = new MqMessage {
				new MqFrame(new byte[1], MqFrameType.Last)
			};

			Server.NewSessionConnected += session => {
				session.Send(message_source);
			};

			Client.IncomingMessage += (sender, args) => {
				MqMessage message;
				args.Mailbox.Inbox.TryDequeue(out message);
				TestStatus.Set();
			};

			StartAndWait();
		}

		[Fact]
		public void Client_receives_valid_message_from_server() {
			var message_source = GenerateRandomMessage();

			Server.NewSessionConnected += session => {
				session.Send(message_source);
			};

			Client.IncomingMessage += (sender, args) => {
				MqMessage message;
				args.Mailbox.Inbox.TryDequeue(out message);

				CompareMessages(message_source, message);

				TestStatus.Set();
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
		public void Client_notified_server_session_closed() {

			Server.NewSessionConnected += session => session.Close(CloseReason.Unknown);

			Client.Closed += (sender, args) => TestStatus.Set();

			StartAndWait();
		}

	}
}
