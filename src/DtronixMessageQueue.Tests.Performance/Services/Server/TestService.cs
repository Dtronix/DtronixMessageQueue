using System;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.Tests.Performance.Services.Server
{
    class TestService : MarshalByRefObject, ITestService
    {
        public string Name { get; } = "TestService";
        public SimpleRpcSession Session { get; set; }

        public event EventHandler<SimpleRpcSession> Completed;

        private int _callCount = 0;
        private int _totalCalls = 0;

        private bool _completed = false;

        public void TestNoReturn()
        {
            var number = Interlocked.Increment(ref _callCount);
            //Console.Write($"{Thread.CurrentThread.ManagedThreadId} ");

            VerifyComplete();
        }

        public async void TestNoReturnAwait()
        {
            var number = Interlocked.Increment(ref _callCount);
            await Task.Delay(1000);

            VerifyComplete();
        }

        public void TestNoReturnLongBlocking()
        {
            var number = Interlocked.Increment(ref _callCount);
            Thread.Sleep(10000);
            VerifyComplete();
        }


        public void TestNoReturnBlock()
        {
            Task.Factory.StartNew(() =>
            {
                var number = Interlocked.Increment(ref _callCount);

                Thread.Sleep(1000);
                VerifyComplete();
            }, TaskCreationOptions.LongRunning);
        }

        public int TestIncrement()
        {
            _callCount++;
            VerifyComplete();
            return _callCount;
        }

        public void TestSetup(int calls)
        {
            _totalCalls = calls;
        }

        public bool ResetTest()
        {
            _callCount = 0;
            _completed = false;
            return true;
        }

        public int TestException()
        {
            _callCount++;
            VerifyComplete();
            throw new Exception("This is a test exception");
        }

        private void VerifyComplete()
        {
            if (_completed == false && _totalCalls == _callCount)
            {
                _completed = true;
                Completed?.Invoke(this, Session);
            }
        }
    }

    internal interface ITestService : IRemoteService<SimpleRpcSession, RpcConfig>
    {
        void TestNoReturn();
        void TestNoReturnAwait();
        void TestNoReturnBlock();
        void TestNoReturnLongBlocking();
        int TestIncrement();
        void TestSetup(int calls);
        bool ResetTest();
        int TestException();
    }
}