using System;
using System.Diagnostics;
using System.Threading;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Rpc.Services.Server;
using NUnit.Framework;


namespace DtronixMessageQueue.Tests.Rpc
{
    public class RpcClientTests : RpcTestsBase
    {
        public RpcClientTests()
        {
        }

        public class Test
        {
            public string TestStr { get; set; }
            public int Length { get; set; }
        }

        [Test]
        public void Client_calls_proxy_method()
        {
            Server.Ready += (sender, args) => { args.Session.AddService(new CalculatorService()); };

            Client.Authenticate += (sender, args) => { };


            Client.Ready += (sender, args) =>
            {
                args.Session.AddProxy<ICalculatorService>("CalculatorService");
                var service = Client.Session.GetProxy<ICalculatorService>();
                var result = service.Add(100, 200);

                if (result != 300)
                {
                    LastException = new Exception("Service returned wrong result.");
                }

                TestStatus.Set();
            };

            StartAndWait();
        }

        [Test]
        public void Client_calls_proxy_method_sequential()
        {
            Server.SessionSetup += (sender, args) => { args.Session.AddService(new CalculatorService()); };


            Client.Ready += (sender, args) =>
            {
                args.Session.AddProxy<ICalculatorService>("CalculatorService");
                var service = Client.Session.GetProxy<ICalculatorService>();
                Stopwatch stopwatch = Stopwatch.StartNew();

                int addedInt = 0;
                for (int i = 0; i < 10; i++)
                {
                    addedInt = service.Add(addedInt, 1);
                }

                Console.WriteLine($"{stopwatch.ElapsedMilliseconds}");
                TestStatus.Set();
            };

            StartAndWait();
        }

        [Test]
        public void Client_calls_proxy_method_and_canceles()
        {
            Server.SessionSetup += (sender, args) =>
            {
                var service = new CalculatorService();
                args.Session.AddService<ICalculatorService>(service);

                service.LongRunningTaskCanceled += (o, eventArgs) => { TestStatus.Set(); };
            };


            Client.Ready += (sender, args) =>
            {
                args.Session.AddProxy<ICalculatorService>("CalculatorService");
                var service = Client.Session.GetProxy<ICalculatorService>();
                var tokenSource = new CancellationTokenSource();


                bool threw = false;
                try
                {
                    tokenSource.CancelAfter(500);
                    service.LongRunningTask(1, 2, tokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    threw = true;
                }

                if (threw != true)
                {
                    LastException = new Exception("Operation did not cancel.");
                }
            };

            StartAndWait();
        }

        [Test]
        public void Server_requests_authentication()
        {
            Server.Config.RequireAuthentication = true;


            Client.Authenticate += (sender, e) => { TestStatus.Set(); };


            StartAndWait();
        }

        [Test]
        public void Server_does_not_request_authentication()
        {
            Server.Config.RequireAuthentication = false;

            Server.SessionSetup +=
                (sender, args) => { args.Session.AddService<ICalculatorService>(new CalculatorService()); };

            Client.Authenticate += (sender, e) => { };


            Client.Ready += (sender, e) =>
            {
                e.Session.AddProxy<ICalculatorService>("CalculatorService");
                var service = Client.Session.GetProxy<ICalculatorService>();

                var result = service.Add(100, 200);

                if (result != 300)
                {
                    LastException = new Exception("Client authenticated.");
                }
                TestStatus.Set();
            };


            StartAndWait();
        }


        [Test]
        public void Server_verifies_authentication()
        {
            var authData = new byte[] {1, 2, 3, 4, 5};

            Server.Config.RequireAuthentication = true;

            Server.SessionSetup +=
                (sender, args) => { args.Session.AddService<ICalculatorService>(new CalculatorService()); };

            Server.Authenticate += (sender, e) =>
            {
                try
                {
                    Assert.AreEqual(authData, e.AuthData);
                }
                catch (Exception ex)
                {
                    LastException = ex;
                }
                finally
                {
                    TestStatus.Set();
                }
            };

            Client.Authenticate += (sender, e) => { e.AuthData = authData; };


            StartAndWait();
        }

        [Test]
        public void Server_disconnectes_from_failed_authentication()
        {
            Server.Config.RequireAuthentication = true;

            Server.SessionSetup +=
                (sender, args) => { args.Session.AddService<ICalculatorService>(new CalculatorService()); };

            Server.Authenticate += (sender, e) => { e.Authenticated = false; };

            Server.Closed += (sender, e) =>
            {
                if (e.CloseReason != SocketCloseReason.AuthenticationFailure)
                {
                    LastException = new Exception("Server closed session for invalid reason");
                }
                TestStatus.Set();
            };

            Client.Authenticate += (sender, e) => { e.AuthData = new byte[] {5, 4, 3, 2, 1}; };

            StartAndWait();
        }

        [Test]
        public void Client_disconnectes_from_failed_authentication()
        {
            Server.Config.RequireAuthentication = true;

            Server.SessionSetup +=
                (sender, args) => { args.Session.AddService<ICalculatorService>(new CalculatorService()); };

            Server.Authenticate += (sender, e) => { e.Authenticated = false; };

            Client.Closed += (sender, e) =>
            {
                if (e.CloseReason != SocketCloseReason.AuthenticationFailure)
                {
                    LastException = new Exception("Server closed session for invalid reason");
                }
                TestStatus.Set();
            };

            Client.Authenticate += (sender, e) => { e.AuthData = new byte[] {5, 4, 3, 2, 1}; };

            StartAndWait();
        }


        [Test]
        public void Client_notified_of_authentication_success()
        {
            Server.Config.RequireAuthentication = true;

            Server.SessionSetup +=
                (sender, args) => { args.Session.AddService<ICalculatorService>(new CalculatorService()); };

            Server.Authenticate += (sender, e) => { e.Authenticated = true; };

            Client.Ready += (sender, e) =>
            {
                if (e.Session.Authenticated == false)
                {
                    LastException = new Exception("Client notified of authentication wrongly.");
                }
                TestStatus.Set();
            };

            Client.Authenticate += (sender, e) => { e.AuthData = new byte[] {5, 4, 3, 2, 1}; };

            StartAndWait();
        }

        [Test]
        public void Client_times_out_on_long_auth()
        {
            Server.Config.RequireAuthentication = true;
            Client.Config.ConnectionTimeout = 100;

            Client.Closed += (sender, e) =>
            {
                if (e.CloseReason != SocketCloseReason.TimeOut)
                {
                    LastException = new Exception("Client was not notified that the authentication failed.");
                }
                TestStatus.Set();
            };

            Server.Authenticate += (sender, e) => { Thread.Sleep(500); };

            StartAndWait(true, 5000, true);
        }

        [Test]
        public void Client_does_not_ready_before_server_authenticates()
        {
            Server.Config.RequireAuthentication = true;

            Server.Authenticate += (sender, args) =>
            {
                args.Authenticated = true;
                Thread.Sleep(200);
            };

            Client.Ready += (sender, args) =>
            {
                if (!args.Session.Authenticated)
                {
                    LastException = new Exception("Client ready event called while authentication failed.");
                }
                TestStatus.Set();
            };

            Client.Authenticate += (sender, e) => { e.AuthData = new byte[] {5, 4, 3, 2, 1}; };

            StartAndWait();
        }

        [Test]
        public void Client_connects_disconnects_and_reconnects()
        {
            Server.Config.RequireAuthentication = false;
            int connectedTimes = 0;


            Client.Ready += (sender, args) =>
            {
                if (!args.Session.Authenticated)
                {
                    LastException = new Exception("Client ready event called while authentication failed.");
                }

                if (++connectedTimes == 5)
                {
                    TestStatus.Set();
                }
                else
                {
                    Client.Close();
                }
            };

            Client.Closed += (sender, args) =>
            {
                if (!TestStatus.IsSet)
                {
                    Client.Connect();
                }
            };

            StartAndWait();
        }
    }
}