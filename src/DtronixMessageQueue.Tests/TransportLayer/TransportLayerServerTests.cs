using System;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Tests.Mq;
using DtronixMessageQueue.TransportLayer;
using DtronixMessageQueue.TransportLayer.Tcp;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.TransportLayer
{
    public class TransportLayerServerTests : TransportLayerTestsBase
    {
        public TransportLayerServerTests()
        {
        }


        [Test]
        public void Server_starts()
        {
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Started)
                    TestComplete.Set();
            };

            StartAndWait();
        }

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
        public void Server_disconnects_client_connection()
        {
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Started)
                    Client.Connect();

                if (args.State == TransportLayerState.Connected)
                    args.Session.Close();

                if (args.State == TransportLayerState.Closed)
                    TestComplete.Set();
            };

            StartAndWait(true, -1, true, false);
        }

        [Test]
        public void Server_notifies_client_of_close()
        {
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.Close();
            };

            Client.Received += (sender, args) =>
            {
                if (args.Buffer == null)
                    TestComplete.Set();
                else
                    LastException = new Exception("Client did not receive a close notification.");
            };

            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.ReceiveAsync();
            };

            StartAndWait();
        }

        [Test]
        public void Server_notifies_client_of_stopping()
        {
            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    Server.Stop();
            };

            Client.Received += (sender, args) =>
            {
                if (args.Buffer == null)
                    TestComplete.Set();
                else
                    LastException = new Exception("Client did not receive a close notification.");
            };

            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.ReceiveAsync();
            };

            StartAndWait();
        }

        [Test]
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
                    Assert.AreEqual(randomBytes, args.Buffer);
                    TestComplete.Set();
                }
                catch (Exception e)
                {
                    LastException = e;
                }
            };

            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.ReceiveAsync();
            };

            StartAndWait();
        }


        [Test]
        public void Server_stops_accepting_connections()
        {
            int connectedCount = 0;
            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                {
                    args.Session.Close();
                    Client.Connect();
                }
            };

            Server.StateChanged += async (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                {
                    if (++connectedCount == 2)
                    {
                        LastException = new Exception("Server accepted new connection when it should not.");
                    }

                    await Task.Delay(300);
                    TestComplete.Set();
                }
            };

            StartAndWait();
        }

        [Test]
        public void Server_accepts_additional_connections()
        {
            int connectedCount = 0;
            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                {
                    args.Session.Close();
                    Client.Connect();
                }
            };

            Server.StateChanged += async (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                {
                    if (++connectedCount == 2)
                        TestComplete.Set();

                    Server.AcceptAsync();

                    await Task.Delay(500);
                    LastException = new Exception("Server accepted new connection when it should not.");
                   
                }
            };

            StartAndWait();
        }
    }
}