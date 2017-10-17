using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.TransportLayer;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected Random Random = new Random();

        public ITestOutputHelper Output;
        private Exception _lastException;

        public int Port { get; }

        public Exception LastException
        {
            get => _lastException;
            set
            {
                _lastException = value;
                TestComplete.Set();
            }
        }

        public TimeSpan TestTimeout { get; } = new TimeSpan(0, 0, 0, 0, 2000);

        public ManualResetEventSlim TestComplete { get; } = new ManualResetEventSlim(false);

        protected TestBase(ITestOutputHelper output)
        {
            Output = output;
            Port = FreeTcpPort();
        }

       

        public static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        protected abstract void StopClientServer();

        protected void StartAndWait(bool timeoutError = true, int timeoutLength = -1)
        {
            timeoutLength = timeoutLength != -1 ? timeoutLength : (int)TestTimeout.TotalMilliseconds;
#if false
            timeoutLength = 100000;
#endif


            TestComplete.Wait(TimeSpan.FromMilliseconds(timeoutLength));

            if (timeoutError && TestComplete.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }

            if (LastException != null)
            {
                throw LastException;
            }

            StopClientServer();
        }

        public void Dispose()
        {
            TestComplete?.Dispose();
            StopClientServer();
        }
    }
}
