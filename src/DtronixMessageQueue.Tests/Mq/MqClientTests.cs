using System;
using System.Threading;
using DtronixMessageQueue.Socket;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests.Mq {

	public class MqClientTests : MqTestsBase {

		public MqClientTests(ITestOutputHelper output) : base(output) {

		}

		[Theory]
		[InlineData(1, false)]
		[InlineData(1, true)]
		[InlineData(100, true)]
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

				while (args.Messages.Count > 0) {
					message = args.Messages.Dequeue();
					Interlocked.Increment(ref received_messages);
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
			var message_source = GenerateRandomMessage(2, 50);

			Client.Connected += (sender, args) => {
				Client.Send(new MqMessage());
				Client.Send(message_source);
			};

			Server.IncomingMessage += (sender, args) => {
				MqMessage message;

				while (args.Messages.Count > 0) {
					message = args.Messages.Dequeue();
					if (message.Count != 2) {
						LastException = new Exception("Server received an empty message.");
					}
					TestStatus.Set();
				}

				
			};

			StartAndWait();
		}

		[Fact]
		public void Client_does_not_notify_on_command_frame() {

			var command_frame = new MqFrame(new byte[21], MqFrameType.Command, Config);

			Client.Connected += (sender, args) => {
				Client.Send(command_frame);
			};

			Server.IncomingMessage += (sender, args) => {
				TestStatus.Set();
			};

			StartAndWait(false, 500);

			if (TestStatus.IsSet) {
				throw new Exception("Server read command frame.");
			}
		}

		[Fact]
		public void Client_does_not_notify_on_ping_frame() {
			var command_frame = new MqFrame(null, MqFrameType.Ping, Config);

			Client.Connected += (sender, args) => {
				Client.Send(command_frame);
			};

			Server.IncomingMessage += (sender, args) => {
				TestStatus.Set();
			};

			StartAndWait(false, 500);

			if (TestStatus.IsSet) {
				throw new Exception("Server read command frame.");
			}
		}


		[Fact]
		public void Client_connects_to_server() {

			Client.Connected += (sender, args) => TestStatus.Set();

			StartAndWait();
		}


		[Fact]
		public void Client_disconects_from_server() {

			Client.Connected += (sender, args) => {
				Client.Close();
			};

			Client.Closed += (sender, args) => TestStatus.Set();

			StartAndWait(true, 500000);
		}

		[Fact]
		public void Client_notified_server_stopping() {

			Server.Connected += (sender, session) => Server.Stop();

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

			Server.Connected += (sender, session) => {
				//Thread.Sleep(1000);
				//session.Session.Send(new MqMessage(new MqFrame(new byte[24], MqFrameType.Last)));
				session.Session.Close(SocketCloseReason.ApplicationError);
			};

			Client.Closed += (sender, args) => {
				if (args.CloseReason != SocketCloseReason.ApplicationError) {
					LastException = new InvalidOperationException("Server did not return proper close reason.");
				}
				TestStatus.Set();
			};

			StartAndWait();
		}

		[Fact]
		public void Client_notifies_server_closing_session() {

			Client.Connected += (sender, args) => Client.Close();

			Server.Closed += (sender, args) => TestStatus.Set();

			StartAndWait();
		}

		[Fact]
		public void Client_times_out() {
			var client_config = new MqConfig {
				Ip = Config.Ip,
				Port = Config.Port,
				PingFrequency = 60000
			};

			
			Client = new MqClient<SimpleMqSession, MqConfig> (client_config);

			Config.PingTimeout = 500;
			Server = new MqServer<SimpleMqSession, MqConfig>(Config);


			Client.Closed += (sender, args) => {
				if (args.CloseReason == SocketCloseReason.TimeOut) {
					TestStatus.Set();
				} else {
					LastException = new Exception("Client closed for reason other than timeout.");
				}
			};

			StartAndWait(false, 2000);

			if (TestStatus.IsSet == false) {
				throw new Exception("Socket did not timeout.");
			}

		}

		[Fact]
		public void Client_prevents_times_out() {
			var client_config = new MqConfig {
				Ip = Config.Ip,
				Port = Config.Port,
				PingFrequency = 100
			};


			Client = new MqClient<SimpleMqSession, MqConfig>(client_config);

			Config.PingTimeout = 200;
			Server = new MqServer<SimpleMqSession, MqConfig>(Config);


			Client.Closed += (sender, args) => {
				if (args.CloseReason == SocketCloseReason.TimeOut) {
					TestStatus.Set();
				} else {
					LastException = new Exception("Client closed for reason other than timeout.");
				}
			};

			StartAndWait(false, 1500);

			if (TestStatus.IsSet) {
				throw new Exception("Client timed out.");
			}
		}

		[Fact]
		public void Client_times_out_after_server_dropped_session() {
			Config.PingTimeout = 500;
			Client = new MqClient<SimpleMqSession, MqConfig>(Config);

			
			Server = new MqServer<SimpleMqSession, MqConfig>(Config);

			Server.Connected += (sender, args) => {
				args.Session.Socket.Close();
			};


			Client.Closed += (sender, args) => {
				if (args.CloseReason == SocketCloseReason.TimeOut) {
					TestStatus.Set();
				} else {
					LastException = new Exception("Client closed for reason other than timeout.");
				}
			};

			StartAndWait(false, 1000);

			if (TestStatus.IsSet == false) {
				throw new Exception("Socket did not timeout.");
			}

		}

	}
}
