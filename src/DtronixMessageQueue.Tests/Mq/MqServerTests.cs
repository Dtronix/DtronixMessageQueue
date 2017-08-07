using System;
using System.Threading;
using DtronixMessageQueue.Socket;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests.Mq
{
    public class MqServerTests : MqTestsBase
    {
        public MqServerTests(ITestOutputHelper output) : base(output)
        {
        }


        [Theory]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(100, true)]
        [InlineData(1000, true)]
        public void Server_should_send_data_to_client(int number, bool validate)
        {
            var messageSource = GenerateRandomMessage(4, 50);

            Server.Connected += (sender, session) =>
            {
                for (int i = 0; i < number; i++)
                {
                    session.Session.Send(messageSource);
                }
            };

            int clientMessageCount = 0;
            Client.IncomingMessage += (sender, args) =>
            {
                MqMessage message;

                clientMessageCount += args.Messages.Count;

                while (args.Messages.Count > 0)
                {
                    message = args.Messages.Dequeue();

                    if (validate)
                    {
                        CompareMessages(messageSource, message);
                    }
                }

                if (clientMessageCount == number)
                {
                    TestStatus.Set();
                }
            };

            StartAndWait();
        }

        [Fact]
        public void Server_accepts_new_connection()
        {
            Server.Connected += (sender, session) => { TestStatus.Set(); };

            StartAndWait();
        }

        [Fact]
        public void Server_detects_client_disconnect()
        {
            Client.Connected += (sender, args) => { Client.Close(); };

            Server.Closed += (session, value) => { TestStatus.Set(); };

            StartAndWait();
        }


        [Fact]
        public void Server_stops()
        {
            Server.Start();
            Assert.Equal(true, Server.IsRunning);
            Server.Stop();
            Assert.Equal(false, Server.IsRunning);
        }

        [Fact]
        public void Server_accepts_new_connection_after_max()
        {
            Server.Config.MaxConnections = 1;

            var client = CreateClient(Config);
            var client2 = CreateClient(Config);

            Server.Start();

            client.Connected += (sender, args) => client.Close();
            client.Closed += (sender, args) => client2.Connect();
            client2.Connected += (sender, args) => TestStatus.Set();
            client.Connect();

            TestStatus.Wait(new TimeSpan(0, 0, 0, 0, 1000));

            if (TestStatus.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }
        }

        [Fact]
        public void Server_refuses_new_connection_after_max()
        {
            Server.Config.MaxConnections = 1;
            Exception invalidClosException = null;
            var client = CreateClient(Config);
            var client2 = CreateClient(Config);

            Server.Start();

            client.Connected += (sender, args) => client2.Connect();
            client2.Closed += (sender, args) =>
            {
                if (args.CloseReason != SocketCloseReason.ConnectionRefused)
                {
                    invalidClosException = new Exception("Client socket did not close for the correct reason.");
                }
                TestStatus.Set();
            };
            client.Connect();
            

            TestStatus.Wait(new TimeSpan(0, 0, 0, 0, 1000));

            if (TestStatus.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }

            if (invalidClosException != null)
            {
                throw invalidClosException;
            }
        }

        [Fact]
        public void Server_restarts_after_stop()
        {
            int connected_times = 0;
            Server.Start();

            Client.Connected += (sender, args) =>
            {
                Server.Stop();
                Client.Close();
                if (++connected_times == 2)
                {
                    TestStatus.Set();
                }
                else
                {
                    Server.Start();
                }
            };


            Client.Closed += (sender, args) =>
            {
                if (connected_times < 2)
                {
                    Client.Connect();
                }
            };

            Client.Connect();



            TestStatus.Wait(new TimeSpan(0, 0, 0, 0, 2000));

            if (TestStatus.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }
        }

        [Fact]
        public void Server_invokes_stopped_event()
        {

            Server.Started += (sender, args) => Server.Stop();
            Server.Started += (sender, args) => TestStatus.Set();

            Server.Start();

            TestStatus.Wait(new TimeSpan(0, 0, 0, 0, 2000));

            if (TestStatus.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }
        }

        [Fact]
        public void Server_invokes_started_event()
        {

            Server.Started += (sender, args) => TestStatus.Set();

            Server.Start();

            TestStatus.Wait(new TimeSpan(0, 0, 0, 0, 2000));

            if (TestStatus.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }
        }

    }
}