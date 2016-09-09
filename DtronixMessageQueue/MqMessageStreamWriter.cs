using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DtronixMessageQueue {

	/// <summary>
	/// Builder to aid in the creation of messages and their frames.
	/// </summary>
	public class MqMessageStreamWriter : Stream {
		public override void Flush() {
			return;
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotImplementedException();
		}

		public override void SetLength(long value) {
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count) {
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			throw new NotImplementedException();
		}

		public override bool CanRead { get; }
		public override bool CanSeek { get; }
		public override bool CanWrite { get; }
		public override long Length { get; }
		public override long Position { get; set; }
	}
}
