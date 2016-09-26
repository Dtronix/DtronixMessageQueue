using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc {
	public class RpcStream<TSession, TConfig> : Stream
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		private readonly RpcSession<TSession, TConfig> session;

		private readonly ushort? read_id;


		private MqMessageReader reader;
		private MqMessageWriter writer;

		public override bool CanRead { get; }
		public override bool CanSeek { get; }
		public override bool CanWrite { get; }
		public override long Length { get; }
		public override long Position { get; set; }

		/// <summary>
		/// Create a RpcStream in writer mode.
		/// </summary>
		/// <param name="session">Session to write to.</param>
		public RpcStream(RpcSession<TSession, TConfig> session) {
			this.session = session;
		}


		/// <summary>
		/// Create a RpcStream in reader mode.
		/// </summary>
		/// <param name="session">Session to read from.</param>
		/// <param name="read_id">Id used to read from the session with.</param>
		public RpcStream(RpcSession<TSession, TConfig> session, ushort read_id) {
			this.session = session;
			this.read_id = read_id;
		}

		/// <summary>
		/// All data is immediately flushed automatically.  Calling flush does nothing.
		/// </summary>
		public override void Flush() {
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
