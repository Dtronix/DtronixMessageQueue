using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc.MessageHandlers;

namespace DtronixMessageQueue.Rpc {
	public class RpcStream<TSession, TConfig> : Stream
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		private readonly RpcSession<TSession, TConfig> session;

		public readonly ushort Id;


		public override bool CanRead { get; }
		public override bool CanSeek { get; } = false;
		public override bool CanWrite { get; }
		public override long Length { get; } = -1;
		public override long Position { get; set; } = -1;
		public override int ReadTimeout { get; set; } = -1;
		public override bool CanTimeout { get; } = false;
		public override int WriteTimeout { get; set; } = -1;

		private ByteTransport<TSession, TConfig> transport;

		private ByteTransportMessageHandler<TSession, TConfig> handler;

		/// <summary>
		/// Create a RpcStream in writer mode.
		/// </summary>
		/// <param name="session">Session to write to.</param>
		public RpcStream(RpcSession<TSession, TConfig> session) {
			this.session = session;
			handler = session.MessageHandlers[2] as ByteTransportMessageHandler<TSession, TConfig>;
			transport = handler.CreateTransport();
			Id = transport.Id;

			CanRead = false;
			CanWrite = true;

		}


		/// <summary>
		/// Create a RpcStream in reader mode.
		/// </summary>
		/// <param name="session">Session to read from.</param>
		/// <param name="read_id">Id used to read from the session with.</param>
		public RpcStream(RpcSession<TSession, TConfig> session, ushort read_id) {
			handler = session.MessageHandlers[2] as ByteTransportMessageHandler<TSession, TConfig>;
			transport = handler.GetRecieveTransport(read_id);

			Id = read_id;
			CanRead = true;
			CanWrite = false;
		}

		/// <summary>
		/// All data is immediately flushed automatically.  Calling flush does nothing.
		/// </summary>
		public override void Flush() {
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotImplementedException();
		}

		public override void SetLength(long value) {
			throw new NotImplementedException();
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
			throw new NotImplementedException();
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
			throw new NotImplementedException();
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		public override int EndRead(IAsyncResult asyncResult) {
			throw new NotImplementedException();
		}

		public override void EndWrite(IAsyncResult asyncResult) {
			throw new NotImplementedException();
		}

		public override Task FlushAsync(CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		public override int ReadByte() {
			throw new NotImplementedException();
		}

		public override void WriteByte(byte value) {
			throw new NotImplementedException();
		}

		public override void Close() {
			transport.Close();
		}

		protected override void Dispose(bool disposing) {
			transport.Close();
		}


		public override int Read(byte[] buffer, int offset, int count) {
			var read = transport.ReadAsync(buffer, offset, count, CancellationToken.None);
			read.Wait();
			return read.Result;
		}

		public override void Write(byte[] buffer, int offset, int count) {
			var read = transport.WriteAsync(buffer, offset, count, CancellationToken.None);
			read.Wait();
		}

		public new Task WriteAsync(byte[] buffer, int offset, int count) {
			return transport.WriteAsync(buffer, offset, count, CancellationToken.None);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellation_token) {
			return transport.WriteAsync(buffer, offset, count, cancellation_token);
		}

		public new Task<int> ReadAsync(byte[] buffer, int offset, int count) {
			return transport.ReadAsync(buffer, offset, count, CancellationToken.None);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation_token) {
			return transport.ReadAsync(buffer, offset, count, cancellation_token);
		}
	}
}
