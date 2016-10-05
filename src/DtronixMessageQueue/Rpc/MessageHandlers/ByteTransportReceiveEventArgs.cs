using System;

namespace DtronixMessageQueue.Rpc.MessageHandlers {
	public class ByteTransportReceiveEventArgs : EventArgs {
		public byte[] Bytes;

		public ByteTransportReceiveEventArgs(byte[] bytes) {
			Bytes = bytes;
		}
	}
}