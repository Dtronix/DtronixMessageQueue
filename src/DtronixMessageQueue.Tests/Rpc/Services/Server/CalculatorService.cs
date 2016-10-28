using System;
using System.IO;
using System.Threading;
using DtronixMessageQueue.Rpc;
using Xunit;

namespace DtronixMessageQueue.Tests.Rpc.Services.Server {
	public class CalculatorService : MarshalByRefObject, ICalculatorService {
		public string Name { get; } = "CalculatorService";
		public SimpleRpcSession Session { get; set; }

		public event EventHandler LongRunningTaskCanceled;

		public event EventHandler SuccessfulStreamTransport;
		public event EventHandler FailedStreamTransport;

		public byte[] StreamBytes = null;

		public int Add(int number_1, int number_2) {
			return number_1 + number_2;
		}

		public int Subtract(int number_1, int number_2) {
			return number_1 - number_2;
		}

		public int Multiply(int number_1, int number_2) {
			return number_1*number_2;
		}

		public int Divide(int number_1, int number_2) {
			return number_1/number_2;
		}

		public int LongRunningTask(int number_1, int number_2, CancellationToken token) {
			ManualResetEventSlim mre = new ManualResetEventSlim();

			try {
				mre.Wait(token);
			} catch (Exception) {
				LongRunningTaskCanceled?.Invoke(this, EventArgs.Empty);
				throw;
			}

			return number_1/number_2;
		}

		public async void UploadStream(RpcStream<SimpleRpcSession, RpcConfig> stream) {

			var ms = new MemoryStream();
			int read;
			byte[] buffer = new byte[128];
			while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
				ms.Write(buffer, 0, read);
			}

			try {
				Assert.Equal(StreamBytes, ms.ToArray());
				SuccessfulStreamTransport?.Invoke(this, EventArgs.Empty);
			} catch (Exception) {
				FailedStreamTransport?.Invoke(this, EventArgs.Empty);
			}
			

		}
	}

	public interface ICalculatorService : IRemoteService<SimpleRpcSession, RpcConfig> {
		int Add(int number_1, int number_2);
		int Subtract(int number_1, int number_2);
		int Multiply(int number_1, int number_2);
		int Divide(int number_1, int number_2);
		int LongRunningTask(int number_1, int number_2, CancellationToken token);
		void UploadStream(RpcStream<SimpleRpcSession, RpcConfig> stream);
	}
}
