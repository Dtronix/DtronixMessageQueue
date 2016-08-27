using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests {


	public class MqServerTests : MqTestsBase {

		public MqServerTests(ITestOutputHelper output) : base(output) {

		}


		[Theory]
		[InlineData(1, false)]
		[InlineData(1, true)]
		[InlineData(50, true)]
		public void Server_should_send_data_to_client(int number, bool validate) {
			var message_source = GenerateRandomMessage(4, 50);

			Server.Connected += (sender, session) => {
				for (int i = 0; i < number; i++) {
					session.Session.Send(message_source);
				}
				
			};

			Client.IncomingMessage += (sender, args) => {
				MqMessage message;
				args.Mailbox.Inbox.TryDequeue(out message);

				if (validate) {
					CompareMessages(message_source, message);
				}

				TestStatus.Set();
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


		//[Fact]
		/*public void Server_stops() {
			Server.Started += (sender, args) => {
				Server.Stop();

				try {
					Assert.Equal(Server.State, ServerState.NotStarted);
					TestStatus.Set();
				} catch (Exception e) {
					LastException = e;
					TestStatus.Set();
				}
				
			};

			StartAndWait();
		}*/
	}
}
