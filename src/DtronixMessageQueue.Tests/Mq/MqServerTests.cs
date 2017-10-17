using System;
using System.Threading;
using DtronixMessageQueue.TransportLayer;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Mq
{
    public class MqServerTests : MqTestsBase
    {


        [TestCase(1, false)]
        [TestCase(1, true)]
        [TestCase(100, true)]
        [TestCase(1000, true)]
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
                    TestComplete.Set();
                }
            };

            StartAndWait();
        }

        [Test]
        public void Server_accepts_new_connection()
        {
            Server.Connected += (sender, session) => { TestComplete.Set(); };

            StartAndWait();
        }

        [Test]
        public void Server_detects_client_disconnect()
        {
            Client.Connected += (sender, args) => { Client.Close(); };

            Server.Closed += (session, value) => { TestComplete.Set(); };

            StartAndWait();
        }


        [Test]
        public void Server_stops()
        {
            Server.Start();
            Assert.AreEqual(TransportLayerState.Started, Server.State);
            Server.Stop();
            Assert.AreEqual(TransportLayerState.Stopped, Server.State);
        }

        [Test]
        public void Server_accepts_new_connection_after_max()
        {
            Server.Config.MaxConnections = 1;

            var client = CreateClient(ClientConfig);
            var client2 = CreateClient(ClientConfig);

            Server.Start();

            client.Connected += (sender, args) => client.Close();
            client.Closed += (sender, args) => client2.Connect();
            client2.Connected += (sender, args) => TestComplete.Set();
            client.Connect();

            TestComplete.Wait(new TimeSpan(0, 0, 0, 0, 1000));

            if (TestComplete.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }
        }

        [Test]
        public void Server_refuses_new_connection_after_max()
        {
            ServerConfig.MaxConnections = 1;
            Server = new MqServer<SimpleMqSession, MqConfig>(ServerConfig);

            Exception invalidClosException = null;
            var client = CreateClient(ClientConfig);
            var client2 = CreateClient(ClientConfig);

            Server.Start();

            client.Connected += (sender, args) => client2.Connect();
            client2.Closed += (sender, args) =>
            {
                if (args.CloseReason != SessionCloseReason.ConnectionRefused)
                {
                    invalidClosException = new Exception("Client socket did not close for the correct reason.");
                }
                TestComplete.Set();
            };
            client.Connect();
            

            TestComplete.Wait(new TimeSpan(0, 0, 0, 0, 8000));

            if (TestComplete.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }

            if (invalidClosException != null)
            {
                throw invalidClosException;
            }
        }

        [Test]
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
                    TestComplete.Set();
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



            TestComplete.Wait(new TimeSpan(0, 0, 0, 0, 2000));

            if (TestComplete.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }
        }

        [Test]
        public void Server_invokes_stopped_event()
        {

            Server.Started += (sender, args) => Server.Stop();
            Server.Started += (sender, args) => TestComplete.Set();

            Server.Start();

            TestComplete.Wait(new TimeSpan(0, 0, 0, 0, 2000));

            if (TestComplete.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }
        }

        [Test]
        public void Server_invokes_started_event()
        {

            Server.Started += (sender, args) => TestComplete.Set();

            Server.Start();

            TestComplete.Wait(new TimeSpan(0, 0, 0, 0, 2000));

            if (TestComplete.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }
        }

    }
}