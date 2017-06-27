using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.Tests.Performance.Services.Server
{
    public interface ICalculatorService : IRemoteService<SimpleRpcSession, RpcConfig>
    {
        int Add(int number1, int number2);
        int Subtract(int number1, int number2);
        int Multiply(int number1, int number2);
        int Divide(int number1, int number2);
    }
}