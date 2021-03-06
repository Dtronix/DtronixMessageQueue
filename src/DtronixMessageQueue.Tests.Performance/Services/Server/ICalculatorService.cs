﻿using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.Tests.Performance.Services.Server {
	public interface ICalculatorService : IRemoteService <SimpleRpcSession, RpcConfig>{
		int Add(int number_1, int number_2);
		int Subtract(int number_1, int number_2);
		int Multiply(int number_1, int number_2);
		int Divide(int number_1, int number_2);
	}
}
