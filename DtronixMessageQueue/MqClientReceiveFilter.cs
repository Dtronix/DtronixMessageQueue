using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.Common;
using SuperSocket.ProtoBase;

namespace DtronixMessageQueue {
	class MqClientReceiveFilter : FixedHeaderReceiveFilter<BufferedPackageInfo> {
		public MqClientReceiveFilter() : base(3) {
		}


		protected override int GetBodyLengthFromHeader(IBufferStream buffer_stream, int length) {
			buffer_stream.ReadByte();
			var sz = buffer_stream.ReadUInt16(true);
			return sz;
		}

		public override BufferedPackageInfo ResolvePackage(IBufferStream buffer_stream) {
			return new BufferedPackageInfo(null, buffer_stream.Buffers);
		}
	}
}