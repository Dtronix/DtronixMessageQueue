using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public static class Utilities {
		public static MqFrame CreateFrame(byte[] bytes, MqFrameType type, MqSocketConfig config) {
			if (type == MqFrameType.Ping || type == MqFrameType.Empty || type == MqFrameType.EmptyLast) {
				bytes = null;
			}
			return new MqFrame(bytes, type, config);
		}
	}
}
