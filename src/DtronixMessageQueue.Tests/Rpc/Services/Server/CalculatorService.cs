using System;
using System.Threading;
using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.Tests.Rpc.Services.Server {
	public class CalculatorService : MarshalByRefObject, ICalculatorService {
		public string Name { get; } = "CalculatorService";
		public SimpleRpcSession Session { get; set; }

		public event EventHandler LongRunningTaskCanceled;

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

			return number_1 / number_2;
		}
	}

	public interface ICalculatorService : IRemoteService<SimpleRpcSession> {
		int Add(int number_1, int number_2);
		int Subtract(int number_1, int number_2);
		int Multiply(int number_1, int number_2);
		int Divide(int number_1, int number_2);
		int LongRunningTask(int number_1, int number_2, CancellationToken token);
	}
}
