using System;
using System.Net.Sockets;
using System.Threading;
using DtronixMessageQueue.TransportLayer;
using DtronixMessageQueue.TransportLayer.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests.Mq
{
    public class MqClientTests : MqTestsBase
    {
        public MqClientTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Client_should_send_data_to_server()
        {
            var messageSource = GenerateRandomMessage(4, 50);
            int receivedMessages = 0;
            var number = 100;
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
                    CompareMessages(messageSource, message);
                }


                if (receivedMessages == number)
                {
                    TestComplete.Set();
                }
            };

            StartAndWait();
        }


        [Fact]
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

        [Fact]
        public void Client_does_not_notify_on_command_frame()
        {
            var commandFrame = new MqFrame(new byte[21], MqFrameType.Command, ClientConfig);

            Client.Connected += (sender, args) =>
            {
                Client.Send(commandFrame);
            };

            Server.IncomingMessage += (sender, args) =>
            {
                TestComplete.Set();
            };

            StartAndWait(false, 500);

            if (TestComplete.IsSet)
            {
                throw new Exception("Server read command frame.");
            }
        }

        [Fact]
        public void Client_does_not_notify_on_ping_frame()
        {
            ServerConfig.PingFrequency = 0;
            ClientConfig.PingFrequency = 0;

            var commandFrame = new MqFrame(new byte[]{1,2,3}, MqFrameType.Ping, ClientConfig);
            var randomMessage = GenerateRandomMessage(1, 10);

            Client.Connected += (sender, args) =>
            {
                Client.Send(commandFrame);
                Client.Send(randomMessage);
            };

            Server.IncomingMessage += (sender, args) =>
            {

                if (CompareMessages(randomMessage, args.Messages.Dequeue()))
                {
                    TestComplete.Set();
                }
                else
                {
                    LastException = new Exception("Server read command frame.");
                }
            };

            StartAndWait(true, 10000);
        }


        [Fact]
        public void Client_connects_to_server()
        {
            Client.Connected += (sender, args) => TestComplete.Set();

            StartAndWait();
        }


        [Fact]
        public void Client_disconects_from_server()
        {
            Client.Connected += (sender, args) => { Client.Close(); };

            Client.Closed += (sender, args) => TestComplete.Set();

            StartAndWait(true, 500000);
        }

        [Fact]
        public void Client_notified_server_stopping()
        {
            Server.Connected += (sender, session) => Server.Stop();

            Client.Closed += (sender, args) => TestComplete.Set();

            StartAndWait();
        }

        [Fact]
        public void Client_closes_self()
        {
            Client.Connected += (sender, args) => Client.Close();

            Client.Closed += (sender, args) => TestComplete.Set();

            StartAndWait();
        }

        [Fact]
        public void Client_notified_server_session_closed()
        {
            Server.Connected += (sender, session) =>
            {
                session.Session.Close(SessionCloseReason.ApplicationError);
            };

            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason != SessionCloseReason.ApplicationError)
                {
                    LastException = new InvalidOperationException("Server did not return proper close reason.");
                }
                TestComplete.Set();
            };

            StartAndWait();
        }

        [Fact]
        public void Client_notifies_server_closing_session()
        {
            Client.Connected += (sender, args) => Client.Close();

            Server.Closed += (sender, args) => TestComplete.Set();

            StartAndWait();
        }

        [Fact]
        public void Client_times_out()
        {
            ClientConfig.PingFrequency = 60000;
            ServerConfig.PingTimeout = 500;

            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == SessionCloseReason.TimeOut)
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

        [Fact]
        public void Client_prevents_times_out()
        {
            ClientConfig.PingFrequency = 100;
            ClientConfig.PingTimeout = 200;


            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == SessionCloseReason.TimeOut)
                {
                    TestComplete.Set();
                }
                else
                {
                    LastException = new Exception("Client closed for reason other than timeout.");
                }
            };

            StartAndWait(false, 1500);

            if (TestComplete.IsSet)
            {
                throw new Exception("Client timed out.");
            }
        }

        [Fact]
        public void Client_times_out_after_server_dropped_session()
        {
            ClientConfig.PingTimeout = 500;

            Server.Connected += (sender, args) =>
            {
                ((TcpTransportLayerSession) args.Session.TransportSession).SimulateConnectionDrop = true;
            };

            Server.Started += (sender, args) =>
            {
                Client.Connect();
            };


            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == SessionCloseReason.TimeOut)
                {
                    TestComplete.Set();
                }
                else
                {
                    LastException = new Exception("Client closed for reason other than timeout.");
                }
            };

            StartAndWait(false, 5000, true, false);

            if (TestComplete.IsSet == false)
            {
                throw new Exception("Socket did not timeout.");
            }
        }


        [Fact]
        public void Client_times_out_while_connecting_for_too_long()
        {
            ClientConfig.ConnectionTimeout = 500;

            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == SessionCloseReason.TimeOut)
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

        [Fact]
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