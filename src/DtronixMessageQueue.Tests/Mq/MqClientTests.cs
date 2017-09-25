using System;
using System.Threading;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.TransportLayer;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests.Mq
{
    public class MqClientTests : MqTestsBase
    {
        public MqClientTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(100, true)]
        [InlineData(1000, true)]
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
                    TestStatus.Set();
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
                    TestStatus.Set();
                }
            };

            StartAndWait();
        }

        [Fact]
        public void Client_does_not_notify_on_command_frame()
        {
            var commandFrame = new MqFrame(new byte[21], MqFrameType.Command, Config);

            Client.Connected += (sender, args) => { Client.Send(commandFrame); };

            Server.IncomingMessage += (sender, args) => { TestStatus.Set(); };

            StartAndWait(false, 500);

            if (TestStatus.IsSet)
            {
                throw new Exception("Server read command frame.");
            }
        }

        [Fact]
        public void Client_does_not_notify_on_ping_frame()
        {
            var commandFrame = new MqFrame(null, MqFrameType.Ping, Config);

            Client.Connected += (sender, args) => { Client.Send(commandFrame); };

            Server.IncomingMessage += (sender, args) => { TestStatus.Set(); };

            StartAndWait(false, 500);

            if (TestStatus.IsSet)
            {
                throw new Exception("Server read command frame.");
            }
        }


        [Fact]
        public void Client_connects_to_server()
        {
            Client.Connected += (sender, args) => TestStatus.Set();

            StartAndWait();
        }


        [Fact]
        public void Client_disconects_from_server()
        {
            Client.Connected += (sender, args) => { Client.Close(); };

            Client.Closed += (sender, args) => TestStatus.Set();

            StartAndWait(true, 500000);
        }

        [Fact]
        public void Client_notified_server_stopping()
        {
            Server.Connected += (sender, session) => Server.Stop();

            Client.Closed += (sender, args) => TestStatus.Set();

            StartAndWait();
        }

        [Fact]
        public void Client_closes_self()
        {
            Client.Connected += (sender, args) => Client.Close();

            Client.Closed += (sender, args) => TestStatus.Set();

            StartAndWait();
        }

        [Fact]
        public void Client_notified_server_session_closed()
        {
            Server.Connected += (sender, session) =>
            {
                //Thread.Sleep(1000);
                //session.Session.Send(new MqMessage(new MqFrame(new byte[24], MqFrameType.Last)));
                session.Session.Close(SessionCloseReason.ApplicationError);
            };

            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason != SessionCloseReason.ApplicationError)
                {
                    LastException = new InvalidOperationException("Server did not return proper close reason.");
                }
                TestStatus.Set();
            };

            StartAndWait();
        }

        [Fact]
        public void Client_notifies_server_closing_session()
        {
            Client.Connected += (sender, args) => Client.Close();

            Server.Closed += (sender, args) => TestStatus.Set();

            StartAndWait();
        }

        [Fact]
        public void Client_times_out()
        {
            var clientConfig = new MqConfig
            {
                Ip = Config.Ip,
                Port = Config.Port,
                PingFrequency = 60000
            };


            Client = new MqClient<SimpleMqSession, MqConfig>(clientConfig);

            Config.PingTimeout = 500;
            Server = new MqServer<SimpleMqSession, MqConfig>(Config);


            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == SessionCloseReason.TimeOut)
                {
                    TestStatus.Set();
                }
                else
                {
                    LastException = new Exception("Client closed for reason other than timeout.");
                }
            };

            StartAndWait(false, 2000);

            if (TestStatus.IsSet == false)
            {
                throw new Exception("Socket did not timeout.");
            }
        }

        [Fact]
        public void Client_prevents_times_out()
        {
            var clientConfig = new MqConfig
            {
                Ip = Config.Ip,
                Port = Config.Port,
                PingFrequency = 100
            };


            Client = new MqClient<SimpleMqSession, MqConfig>(clientConfig);

            Config.PingTimeout = 200;
            Server = new MqServer<SimpleMqSession, MqConfig>(Config);


            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == SessionCloseReason.TimeOut)
                {
                    TestStatus.Set();
                }
                else
                {
                    LastException = new Exception("Client closed for reason other than timeout.");
                }
            };

            StartAndWait(false, 1500);

            if (TestStatus.IsSet)
            {
                throw new Exception("Client timed out.");
            }
        }

        [Fact]
        public void Client_times_out_after_server_dropped_session()
        {
            Config.PingTimeout = 500;
            Client = new MqClient<SimpleMqSession, MqConfig>(Config);


            Server = new MqServer<SimpleMqSession, MqConfig>(Config);

            Server.Connected += (sender, args) => { args.Session.Socket.Close(); };


            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == SessionCloseReason.TimeOut)
                {
                    TestStatus.Set();
                }
                else
                {
                    LastException = new Exception("Client closed for reason other than timeout.");
                }
            };

            StartAndWait(false, 1000);

            if (TestStatus.IsSet == false)
            {
                throw new Exception("Socket did not timeout.");
            }
        }

        [Fact]
        public void Client_times_out_while_connecting_for_too_long()
        {
            Config.ConnectionTimeout = 100;
            Client = new MqClient<SimpleMqSession, MqConfig>(Config);

            Server.Connected += (sender, args) => { args.Session.Socket.Close(); };


            Client.Closed += (sender, args) =>
            {
                if (args.CloseReason == SessionCloseReason.TimeOut)
                {
                    TestStatus.Set();
                }
                else
                {
                    LastException = new Exception("Client closed for reason other than timeout.");
                }
            };

            StartAndWait(false, 10000, false);

            if (TestStatus.IsSet == false)
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
                    TestStatus.Set();
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
            


            TestStatus.Wait(new TimeSpan(0, 0, 0, 0, 2000));

            if (TestStatus.IsSet == false)
            {
                throw new TimeoutException("Test timed out.");
            }
        }
    }
}