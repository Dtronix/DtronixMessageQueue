using System;

namespace DtronixMessageQueue.Tests.Performance.Services.Server
{
    public class CalculatorService : MarshalByRefObject, ICalculatorService
    {
        public string Name { get; } = "CalculatorService";
        public SimpleRpcSession Session { get; set; }

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
    }
}