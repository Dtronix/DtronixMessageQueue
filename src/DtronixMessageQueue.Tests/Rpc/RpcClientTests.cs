using System;
using System.Diagnostics;
using System.Threading;
using DtronixMessageQueue.Tests.Rpc.Services.Server;
using DtronixMessageQueue.TransportLayer;
using NUnit.Framework;


namespace DtronixMessageQueue.Tests.Rpc
{
    public class RpcClientTests : RpcTestsBase
    {

        public class Test
        {
            public string TestStr { get; set; }
            public int Length { get; set; }
        }

        [Test]
        public void Client_calls_proxy_method()
        {
            Server.Connected += (sender, args) => { args.Session.AddService(new CalculatorService()); };

            Client.Connected += (sender, args) =>
            {
                args.Session.AddProxy<ICalculatorService>("CalculatorService");
            };


            Client.Ready += (sender, args) =>
            {
                var service = Client.Session.GetProxy<ICalculatorService>();
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
        public void Client_calls_proxy_method_and_canceles()
        {
            Server.Connected += (sender, args) =>
            {
                var service = new CalculatorService();
                args.Session.AddService<ICalculatorService>(service);

                service.LongRunningTaskCanceled += (o, eventArgs) => { TestComplete.Set(); };
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
            ClientConfig.RequireAuthentication = ServerConfig.RequireAuthentication = true;

            Client.Authenticate += (sender, e) => { TestComplete.Set(); };

            StartAndWait();
        }

        [Test]
        public void Server_does_not_request_authentication()
        {
            ServerConfig.RequireAuthentication = false;

            Server.Connected +=
                (sender, args) => { };

            Client.Authenticate += (sender, e) => { };


            Client.Ready += (sender, e) =>
            {
                TestComplete.Set();
            };


            StartAndWait();
        }


        [Test]
        public void Server_verifies_authentication()
        {
            var authData = new byte[] {1, 2, 3, 4, 5};

            ClientConfig.RequireAuthentication = ServerConfig.RequireAuthentication = true;

            Server.Connected +=
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
                    TestComplete.Set();
                }
            };

            Client.Authenticate += (sender, e) => { e.AuthData = authData; };


            StartAndWait();
        }

        [Test]
        public void Server_disconnectes_from_failed_authentication()
        {
            ClientConfig.RequireAuthentication = ServerConfig.RequireAuthentication = true;

            Server.Connected +=
                (sender, args) =>
                {
                    args.Session.AddService<ICalculatorService>(new CalculatorService()); 
                    
                };

            Server.Authenticate += (sender, e) =>
            {
                e.Authenticated = false;
            };

            Server.Closed += (sender, e) =>
            {
                if (e.CloseReason != SessionCloseReason.AuthenticationFailure)
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
            ClientConfig.RequireAuthentication = ServerConfig.RequireAuthentication = true;

            Server.Connected +=
                (sender, args) =>
                {
                    args.Session.AddService<ICalculatorService>(new CalculatorService());
                };

            Server.Authenticate += (sender, e) =>
            {
                e.Authenticated = false;
            };

            Client.Closed += (sender, e) =>
            {
                if (e.CloseReason != SessionCloseReason.AuthenticationFailure && !IsMono)
                {
                    LastException = new Exception($"{e.CloseReason} Mono:{IsMono} Server closed session for invalid reason");
                }
                TestComplete.Set();
            };

            Client.Authenticate += (sender, e) => { e.AuthData = new byte[] {5, 4, 3, 2, 1}; };

            StartAndWait();
        }


        [Test]
        public void Client_notified_of_authentication_success()
        {
            ClientConfig.RequireAuthentication = ServerConfig.RequireAuthentication = true;

            Server.Connected +=
                (sender, args) => { args.Session.AddService<ICalculatorService>(new CalculatorService()); };

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
            ClientConfig.RequireAuthentication = ServerConfig.RequireAuthentication = true;
            
            ClientConfig.ConnectionTimeout = 100;
            
            Client.Closed += (sender, e) =>
            {
                if (e.CloseReason != SessionCloseReason.AuthenticationFailure)
                {
                    LastException = new Exception($"Client was not notified that the authentication failed.");
                }
                TestComplete.Set();
            };

            Server.Authenticate += (sender, e) =>
            {
                Thread.Sleep(500);
            };

            Server.Started += (sender, args) =>
            {
                Client.Connect();
            };

            StartAndWait(true, 3000, true, false);
        }

        [Test]
        public void Client_does_not_ready_before_server_authenticates()
        {
            ClientConfig.RequireAuthentication = ServerConfig.RequireAuthentication = true;
            var serverAuth = false;
            Server.Authenticate += (sender, args) =>
            {
                args.Authenticated = true;
                Thread.Sleep(100);
                serverAuth = true;
            };

            Client.Ready += (sender, args) =>
            {
                if (!serverAuth)
                {
                    LastException = new Exception("Client ready event called while authentication failed.");
                }
                TestComplete.Set();
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
    }
}