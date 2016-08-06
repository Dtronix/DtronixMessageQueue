using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using DtronixMessageQueue;
using SuperSocket.SocketBase.Config;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueueTests {

	public class MessageQueueClientTests {
		private ITestOutputHelper output;

		public MessageQueueClientTests(ITestOutputHelper output) {
			this.output = output;
		}

		[Fact]
		public async void Client_sends_data_to_server() {
			
		}

	}
}
