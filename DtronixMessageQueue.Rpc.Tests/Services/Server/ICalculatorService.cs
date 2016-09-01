using System;

namespace DtronixMessageQueue.Rpc.Tests.Services.Server {
	public interface ICalculatorService : IRemoteService <SimpleRpcSession>{
		int Add(int number_1, int number_2);
		int Subtract(int number_1, int number_2);
		int Multiply(int number_1, int number_2);
		int Divide(int number_1, int number_2);
	}
}
