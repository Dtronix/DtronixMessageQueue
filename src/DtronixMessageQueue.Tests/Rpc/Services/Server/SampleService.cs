using System;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.Tests.Rpc.Services.Server
{
    public class SampleService : MarshalByRefObject, ISampleService
    {
        public string Name { get; } = "SampleService";
        public SimpleRpcSession Session { get; set; }

        public event EventHandler LongRunningTaskCanceled;

        public byte[] StreamBytes = null;

        public int Add(int number1, int number2)
        {
            return number1 + number2;
        }

        public int Subtract(int number1, int number2)
        {
            return number1 - number2;
        }

        public int Multiply(int number1, int number2)
        {
            return number1 * number2;
        }

        public int Divide(int number1, int number2)
        {
            return number1 / number2;
        }

        public void InvalidArguments(int number1, int number2, CancellationToken token)
        {
        }
    }

    public interface ISampleService : IRemoteService<SimpleRpcSession, RpcConfig>
    {
        int Add(int number1, int number2);
        int Subtract(int number1, int number2);
        int Multiply(int number1, int number2);
        int Divide(int number1, int number2);
        void InvalidArguments(int number1, int number2, CancellationToken token);
    }
}