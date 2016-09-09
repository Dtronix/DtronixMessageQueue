using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DtronixMessageQueue {

	/// <summary>
	/// Builder to aid in the creation of messages and their frames.
	/// </summary>
	public class MqMessageStream : Stream {

		public enum Mode {
			Read,
			Write,
			ReadWrite
		}

		private MqMessageReader reader;
		private MqMessageWriter writer;

		public Mode StreamMode { get; private set; }
		public MqSocketConfig Config { get; set; }

		public override bool CanRead => StreamMode == Mode.Read || StreamMode == Mode.ReadWrite;
		public override bool CanSeek => true;
		public override bool CanWrite => StreamMode == Mode.Write || StreamMode == Mode.ReadWrite;
		public override long Length { get; }
		public override long Position { get; set; }

		public MqMessageStream(Mode mode, MqSocketConfig config) {
			StreamMode = mode;
			Config = config;

			switch (mode) {
				case Mode.Read:
					reader = new MqMessageReader();
					break;
				case Mode.Write:
					writer = new MqMessageWriter(config);
					break;
				case Mode.ReadWrite:
					reader = new MqMessageReader();
					writer = new MqMessageWriter(config);
					break;
			}
		}

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

	}
}
