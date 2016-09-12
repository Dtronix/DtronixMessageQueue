using System;
using System.Threading;

namespace DtronixMessageQueue.Rpc.Tests.Services.Server {
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

			mre.Wait(token);

			if (mre.IsSet == false) {
				LongRunningTaskCanceled?.Invoke(this, EventArgs.Empty);
			}

			return number_1 / number_2;
		}
	}
}
