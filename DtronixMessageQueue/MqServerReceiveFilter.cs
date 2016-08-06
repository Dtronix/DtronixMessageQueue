using System;
using SuperSocket.Common;
using SuperSocket.Facility.Protocol;
using SuperSocket.SocketBase.Protocol;

namespace DtronixMessageQueue {
	internal class MqServerReceiveFilter : FixedHeaderReceiveFilter<RequestInfo<byte, byte[]>> {
		public MqServerReceiveFilter() : base(3) {
		}

		protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length) {
			var bytes = new[] {header[offset+1], header[offset + 2]};
			return BitConverter.ToUInt16(bytes, 0);
		}

		protected override RequestInfo<byte, byte[]> ResolveRequestInfo(ArraySegment<byte> header, byte[] body_buffer, int offset, int length) {
			var bytes = new byte[length];
			Buffer.BlockCopy(body_buffer, offset, bytes, 0, length);
			return new RequestInfo<byte, byte[]>("", header.Array[header.Offset], bytes);
		}
	}

}

