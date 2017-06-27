using System;
using System.Threading;
using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.Tests.Rpc.Services.Server
{
    public class CalculatorService : MarshalByRefObject, ICalculatorService
    {
        public string Name { get; } = "CalculatorService";
        public SimpleRpcSession Session { get; set; }

        public event EventHandler LongRunningTaskCanceled;

        public event EventHandler SuccessfulStreamTransport;
        public event EventHandler FailedStreamTransport;

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

        public int LongRunningTask(int number1, int number2, CancellationToken token)
        {
            ManualResetEventSlim mre = new ManualResetEventSlim();

            try
            {
                mre.Wait(token);
            }
            catch (Exception)
            {
                LongRunningTaskCanceled?.Invoke(this, EventArgs.Empty);
                throw;
            }

            return number1 / number2;
        }
    }

    public interface ICalculatorService : IRemoteService<SimpleRpcSession, RpcConfig>
    {
        int Add(int number1, int number2);
        int Subtract(int number1, int number2);
        int Multiply(int number1, int number2);
        int Divide(int number1, int number2);
        int LongRunningTask(int number1, int number2, CancellationToken token);
    }
}