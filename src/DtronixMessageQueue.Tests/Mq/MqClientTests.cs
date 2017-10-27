using System;
using System.Threading;
using DtronixMessageQueue.TcpSocket;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Mq
{
    public class MqClientTests : MqTestsBase
    {

        [TestCase(1, false)]
        [TestCase(1, true)]
        [TestCase(100, true)]
        [TestCase(1000, true)]
        public void Client_should_send_data_to_server(int number, bool validate)
        {
            var messageSource = GenerateRandomMessage(4, 50);
            int receivedMessages = 0;
            Client.Connected += (sender, args) =>
            {
                for (int i = 0; i < number; i++)
                {
                    Client.Send(messageSource);
                }
            };

            Server.IncomingMessage += (sender, args) =>
            {
                MqMessage message;

                while (args.Messages.Count > 0)
                {
                    message = args.Messages.Dequeue();
                    Interlocked.Increment(ref receivedMessages);
                    if (validate)
                    {
                        CompareMessages(messageSource, message);
                    }
                }


                if (receivedMessages == number)
                {
                    TestComplete.Set();
                }
            };

            StartAndWait();
        }


        [Test]
        public void Client_does_not_send_empty_message()
        {
            var messageSource = GenerateRandomMessage(2, 50);

            Client.Connected += (sender, args) =>
            {
                Client.Send(new MqMessage());
                Client.Send(messageSource);
            };

            Server.IncomingMessage += (sender, args) =>
            {
                MqMessage message;

                while (args.Messages.Count > 0)
                {
                    message = args.Messages.Dequeue();
                    if (message.Count != 2)
                    {
                        LastException = new Exception("Server received an empty message.");
                    }
                    TestComplete.Set();
                }
            };

            StartAndWait();
        }

        [Test]
        public void Client_does_not_notify_on_command_frame()
        {
            var commandFrame = new MqFrame(new byte[21], MqFrameType.Command, ClientConfig);

            Client.Connected += (sender, args) => { Client.Send(commandFrame); };

            Server.IncomingMessage += (sender, args) => { TestComplete.Set(); };

            StartAndWait(false, 500);

            if (TestComplete.IsSet)
            {
                throw new Exception("Server read command frame.");
            }
        }

        [Test]
        public void Client_does_not_notify_on_ping_frame()
        {
            var commandFrame = new MqFrame(null, MqFrameType.Ping, ClientConfig);

            Client.Connected += (sender, args) => { Client.Send(commandFrame); };

            Server.IncomingMessage += (sender, args) => { TestComplete.Set(); };

            StartAndWait(false, 500);

            if (TestComplete.IsSet)
            {
                throw new Exception("Server read command frame.");
            }
        }


        [Test]
        public void Client_connects_to_server()
        {
            Client.Connected += (sender, args) => TestComplete.Set();

            StartAndWait();
        }


        [Test]
        public void Client_disconects_from_server()
        {
            Client.Connected += (sender, args) => { Client.Close(); };

            Client.Closed += (sender, args) => TestComplete.Set();

            StartAndWait(true, 500000);
        }

        [Test]
        public void Client_notified_server_stopping()
        {
            Server.Connected += (sender, session) => Server.Stop();

            Client.Closed += (sender, args) => TestComplete.Set();

            StartAndWait();
        }

        [Test]
        public void Client_closes_self()
        {
            Client.Connected += (sender, args) => Client.Close();

            Client.Closed += (sender, args) => TestComplete.Set();

            StartAndWait();
        }

        [Test]
        public void Client_notified_server_session_closed()
        {
            Server.Connected += (sender, session) =>
            {
                session.Session.Close(CloseReason.ApplicationError);
            };

            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason != CloseReason.ApplicationError && !IsMono)
                {
                    LastException = new InvalidOperationException("Server did not return proper close reason.");
                }
                TestComplete.Set();
            };

            StartAndWait();
        }

        [Test]
        public void Client_notifies_server_closing_session()
        {
            Client.Connected += (sender, args) => Client.Close();

            Server.Closed += (sender, args) => TestComplete.Set();

            StartAndWait();
        }

        [Test]
        public void Client_times_out()
        {
            ClientConfig.PingFrequency = 60000;
            ServerConfig.PingTimeout = 500;

            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == CloseReason.TimeOut || IsMono)
                {
                    TestComplete.Set();
                }
                else
                {
                    LastException = new Exception("Client closed for reason other than timeout.");
                }
            };

            StartAndWait(false, 2000);

            if (TestComplete.IsSet == false)
            {
                throw new Exception("Socket did not timeout.");
            }
        }

        [Test]
        public void Client_prevents_times_out()
        {
            ClientConfig.PingFrequency = 50;
            ServerConfig.PingTimeout = 100;


            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == CloseReason.TimeOut)
                {
                    LastException = new Exception("Client timed out.");
                }
                else
                {
                    LastException = new Exception("Client closed for reason other than timeout.");
                }
            };

            StartAndWait(false, 500);
        }

        [Test]
        public void Client_times_out_after_server_dropped_session()
        {
            ClientConfig.PingTimeout = 500;

            Server.Connected += (sender, args) => { args.Session.Socket.Close(); };

            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == CloseReason.Closing)
                {
                    TestComplete.Set();
                }
                else
                {
                    LastException = new Exception("Client closed for reason other than timeout.");
                }
            };

            StartAndWait(false, 1000);

            if (TestComplete.IsSet == false)
            {
                throw new Exception("Socket did not timeout.");
            }
        }

        [Test]
        public void Client_times_out_while_connecting_for_too_long()
        {
            ClientConfig.ConnectionTimeout = 200;

            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == CloseReason.TimeOut)
                {
                    TestComplete.Set();
                }
                else
                {
                    LastException = new Exception("Client closed for reason other than timeout.");
                }
            };

            StartAndWait(false, 10000, false);

            if (TestComplete.IsSet == false)
            {
                throw new Exception("Socket did not timeout.");
            }
        }

        [Test]
        public void Client_reconnects_after_close()
        {
            int connected_times = 0;
            Server.Start();

            Client.Connected += (sender, args) =>
            {
                if (++connected_times == 2)
                {
                    TestComplete.Set();
                }
                Client.Close();

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
    }
}