using System;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Tests.Mq;
using DtronixMessageQueue.TransportLayer;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests.TransportLayer
{
    public class TransportLayerServerTests : TransportLayerTestsBase
    {
        public TransportLayerServerTests(ITestOutputHelper output) : base(output)
        {
        }


        [Fact]
        public void Server_starts()
        {
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Started)
                    TestComplete.Set();
            };

            StartAndWait();
        }

        [Fact]
        public void Server_stops()
        {
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Started)
                    Server.Stop();

                if(args.State == TransportLayerState.Stopped)
                    TestComplete.Set();

            };

            StartAndWait(true, -1, true, false);
        }

        [Fact]
        public void Server_restarts()
        {
            int starts = 0;
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Started)
                    Server.Stop();

                if (args.State == TransportLayerState.Stopped)
                {
                    if(++starts == 2)
                        TestComplete.Set();
                    else
                        Server.Start();
                }

            };

            StartAndWait(true, -1, true, false);
        }

        [Fact]
        public void Server_accepts_client_connection()
        {
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Started)
                    Client.Connect();

                if (args.State == TransportLayerState.Connected)
                    TestComplete.Set();
            };

            StartAndWait(true, -1, true, false);
        }

        [Fact]
        public void Server_disconnects_client_connection()
        {
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Started)
                    Client.Connect();

                if (args.State == TransportLayerState.Connected)
                    args.Session.Close(SessionCloseReason.Closing);

                if (args.State == TransportLayerState.Closed)
                    TestComplete.Set();
            };

            StartAndWait(true, -1, true, false);
        }

        [Fact]
        public void Server_notifies_client_of_close()
        {
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.Close(SessionCloseReason.Closing);
            };

            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Closed)
                    TestComplete.Set();
            };

            StartAndWait();
        }

        [Fact]
        public void Server_notifies_client_stopping()
        {
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    Server.Stop();
            };

            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Closed)
                    TestComplete.Set();
            };

            StartAndWait();
        }

        [Fact]
        public void Server_sends_data()
        {
            var randomBytes = Utilities.SequentialBytes(128);
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.Send(randomBytes, 0, randomBytes.Length);
            };

            Client.Received += (sender, args) =>
            {
                try
                {
                    Assert.Equal(randomBytes, args.Buffer);
                    TestComplete.Set();
                }
                catch (Exception e)
                {
                    LastException = e;
                }
            };

            StartAndWait();
        }


        [Fact]
        public void Server_stops_accepting_connections()
        {
            int connectedCount = 0;
            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.Close(SessionCloseReason.Closing);
            };

            Server.StateChanged += async (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                {
                    if (++connectedCount == 2)
                    {
                        LastException = new Exception("Server accepted new connection when it should not.");
                    }

                    await Task.Delay(500);
                    TestComplete.Set();
                }



                if (args.State == TransportLayerState.Closed)
                {
                    Client.Connect();
                }
            };

            StartAndWait();
        }

        [Fact]
        public void Server_accepts_additional_connections()
        {
            int connectedCount = 0;
            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.Close(SessionCloseReason.Closing);
            };

            Server.StateChanged += async (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                {
                    if (++connectedCount == 2)
                    {
                        TestComplete.Set();
                    }

                    await Task.Delay(500);
                    LastException = new Exception("Server accepted new connection when it should not.");
                   
                }

                if (args.State == TransportLayerState.Closed)
                {
                    Client.Connect();
                    Server.AcceptAsync();
                }
            };

            StartAndWait();
        }
    }
}