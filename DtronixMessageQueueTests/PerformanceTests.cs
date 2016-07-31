using System;
using System.IO;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueueTests {

	public class PerformanceTests {
		private ITestOutputHelper output;

		public PerformanceTests(ITestOutputHelper output) {
			this.output = output;
		}

		[Fact]
		public void Client_sends_data_to_server() {

		}

		private byte[] RandomBytes(int len) {
			byte[] val = new byte[len];
			Random rand = new Random();
			rand.NextBytes(val);

			return val;
		}


	}
}
