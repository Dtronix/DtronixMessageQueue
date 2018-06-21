using System;
using System.Diagnostics;
using System.Threading;
using DtronixMessageQueue.TcpSocket;
using DtronixMessageQueue.Tests.Rpc.Services.Server;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Rpc
{
    public class RpcTests : RpcTestsBase
    {

        public class Test
        {
            public string TestStr { get; set; }
            public int Length { get; set; }
        }

        [Test]
        public void Client_calls_proxy_method()
        {
            Server.SessionSetup += (sender, args) => { args.Session.AddService(new SampleService()); };

            Client.SessionSetup += (sender, args) =>
            {
                args.Session.AddProxy<ISampleService>("SampleService");
            };


            Client.Ready += (sender, args) =>
            {
                var service = Client.Session.GetProxy<ISampleService>();
                var result = service.Add(100, 200);

                if (result != 300)
                {
                    LastException = new Exception("Service returned wrong result.");
                }

                TestComplete.Set();
            };

            StartAndWait();
        }

        [Test]
        public void Client_calls_proxy_method_sequential()
        {
            Server.SessionSetup += (sender, args) => { args.Session.AddService(new SampleService()); };


            Client.Ready += (sender, args) =>
            {
                args.Session.AddProxy<ISampleService>("SampleService");
                var service = Client.Session.GetProxy<ISampleService>();
                Stopwatch stopwatch = Stopwatch.StartNew();

                int addedInt = 0;
                int totalAdds = 50;
                for (int i = 0; i < totalAdds; i++)
                {
                    addedInt = service.Add(addedInt, 1);
                }


                try
                {
                    Assert.AreEqual(totalAdds, addedInt);
                    TestComplete.Set();
                }
                catch (Exception e)
                {
                    LastException = e;
                }

                
            };

            StartAndWait();
        }

        [Test]
        public void Server_requests_authentication()
        {
            Server.Config.RequireAuthentication = true;

            Client.Authenticate += (sender, e) => { TestComplete.Set(); };

            StartAndWait();
        }

        [Test]
        public void Server_does_not_request_authentication()
        {
            Server.Config.RequireAuthentication = false;

            Server.SessionSetup +=
                (sender, args) => { args.Session.AddService<ISampleService>(new SampleService()); };

            Client.Authenticate += (sender, e) => { };


            Client.Ready += (sender, e) =>
            {
                e.Session.AddProxy<ISampleService>("SampleService");
                var service = Client.Session.GetProxy<ISampleService>();

                var result = service.Add(100, 200);

                if (result != 300)
                {
                    LastException = new Exception("Client authenticated.");
                }
                TestComplete.Set();
            };


            StartAndWait();
        }


        [Test]
        public void Server_verifies_authentication()
        {
            var authData = new byte[] {1, 2, 3, 4, 5};

            Server.Config.RequireAuthentication = true;

            Server.SessionSetup +=
                (sender, args) => { args.Session.AddService<ISampleService>(new SampleService()); };

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
                    TestComplete.Set();
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
                (sender, args) => { args.Session.AddService<ISampleService>(new SampleService()); };

            Server.Authenticate += (sender, e) => { e.Authenticated = false; };

            Server.Closed += (sender, e) =>
            {
                if (e.CloseReason != CloseReason.AuthenticationFailure)
                {
                    LastException = new Exception("Server closed session for invalid reason");
                }
                TestComplete.Set();
            };

            Client.Authenticate += (sender, e) => { e.AuthData = new byte[] {5, 4, 3, 2, 1}; };

            StartAndWait();
        }

        [Test]
        public void Client_disconnectes_from_failed_authentication()
        {
            if (IsMono)
                Assert.Ignore("Skipped non-functional test on mono.");

            Server.Config.RequireAuthentication = true;

            Server.SessionSetup +=
                (sender, args) => { args.Session.AddService<ISampleService>(new SampleService()); };

            Server.Authenticate += (sender, e) => { e.Authenticated = false; };

            Client.Closed += (sender, e) =>
            {
                if (e.CloseReason != CloseReason.AuthenticationFailure)
                {
                    LastException = new Exception("Server closed session for invalid reason");
                }
                TestComplete.Set();
            };

            Client.Authenticate += (sender, e) => { e.AuthData = new byte[] {5, 4, 3, 2, 1}; };

            StartAndWait();
        }


        [Test]
        public void Client_notified_of_authentication_success()
        {
            ServerConfig.RequireAuthentication = true;

            Server.SessionSetup +=
                (sender, args) => { args.Session.AddService<ISampleService>(new SampleService()); };

            Server.Authenticate += (sender, e) => { e.Authenticated = true; };

            Client.Ready += (sender, e) =>
            {
                if (e.Session.Authenticated == false)
                {
                    LastException = new Exception("Client notified of authentication wrongly.");
                }
                TestComplete.Set();
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
                if (e.CloseReason != CloseReason.TimeOut)
                {
                    LastException = new Exception("Client was not notified that the authentication failed.");
                }
                TestComplete.Set();
            };

            Server.Authenticate += (sender, e) => { Thread.Sleep(500); };

            StartAndWait(true, 5000);
        }


        [Test]
        public void Client_connects_disconnects_and_reconnects()
        {
            Server.Config.RequireAuthentication = false;
            int connectedTimes = 0;


            Client.Ready += (sender, args) =>
            {
                if (++connectedTimes == 5)
                {
                    TestComplete.Set();
                }
                else
                {
                    Client.Close();
                }
            };

            Client.Closed += (sender, args) =>
            {
                if (!TestComplete.IsSet)
                {
                    Client.Connect();
                }
            };

            StartAndWait();
        }

        [Test]
        public void Client_throws_on_invalid_argument_passed()
        {
            Client.Ready += (sender, args) =>
            {
                args.Session.AddProxy<ISampleService>("SampleService");
                var service = Client.Session.GetProxy<ISampleService>();
                var tokenSource = new CancellationTokenSource();


                bool threw = false;
                try
                {
                    service.InvalidArguments(1, 2, tokenSource.Token);
                }
                catch (ArgumentException)
                {
                    TestComplete.Set();
                    threw = true;
                }

                if (threw != true)
                {
                    LastException = new Exception("Operation did not throw ArgumentException.");
                }
            };

            StartAndWait();
        }
    }
}