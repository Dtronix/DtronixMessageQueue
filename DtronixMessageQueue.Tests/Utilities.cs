using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DtronixMessageQueue.Tests {
	static class Utilities {

		public static void CompareFrame(MqFrame expected, MqFrame actual) {
			if (expected == null) throw new ArgumentNullException(nameof(expected));
			if (actual == null) throw new ArgumentNullException(nameof(actual));

			Assert.Equal(expected.FrameType, actual.FrameType);
			Assert.Equal(expected.Data, actual.Data);
		}
	}
}
