using System;

namespace DtronixMessageQueue.Tests.Performance.Services.Server {
	public class CalculatorService : MarshalByRefObject, ICalculatorService {
		public string Name { get; } = "CalculatorService";
		public SimpleRpcSession Session { get; set; }

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
	}
}
