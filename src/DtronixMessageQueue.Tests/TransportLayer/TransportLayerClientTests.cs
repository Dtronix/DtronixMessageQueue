﻿using System;
using System.Threading;
using DtronixMessageQueue.Tests.Mq;
using DtronixMessageQueue.TransportLayer;
using DtronixMessageQueue.TransportLayer.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests.TransportLayer
{
    public class TransportLayerClientTests : TransportLayerTestsBase
    {
        public TransportLayerClientTests(ITestOutputHelper output) : base(output)
        {
        }


        [Fact]
        public void Client_connects_to_server()
        {
            Client.StateChanged += (sender, args) =>
            {
                if(args.State == TransportLayerState.Connected)
                    TestComplete.Set();
            };

            StartAndWait();
        }

        [Fact]
        public void Client_reconnects_to_server()
        {
            int connectedCount = 0;
            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.Close(SessionCloseReason.Closing);
            };

            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                {
                    if (++connectedCount == 2)
                        TestComplete.Set();
                }

                if (args.State == TransportLayerState.Closed)
                {
                    Client.Connect();
                    Server.AcceptAsync();
                }
            };

            StartAndWait();
        }


        [Fact]
        public void Client_disconnects_from_server()
        {
            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.Close(SessionCloseReason.Closing);

                if (args.State == TransportLayerState.Closed)
                    TestComplete.Set();
            };

            StartAndWait();
        }

        [Fact]
        public void Client_notifies_server_of_close()
        {
            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.Close(SessionCloseReason.Closing);
            };

            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Closed)
                    TestComplete.Set();
            };

            StartAndWait();
        }

        [Fact]
        public void Client_sends_data()
        {
            var randomBytes = Utilities.SequentialBytes(128);
            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.Send(randomBytes, 0, randomBytes.Length);
            };

            Server.Received += (sender, args) =>
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
        public void Client_times_out_on_long_connection()
        {
            ClientConfig.ConnectionTimeout = 500;

            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Closed)
                {
                    if (args.Reason == SessionCloseReason.TimeOut)
                    {
                        TestComplete.Set();
                    }
                    else
                    {
                        LastException = new Exception("Client closed for the wrong reason.");
                    }
                }
                    
            };

            StartAndWait(false, 10000, false);

            if (TestComplete.IsSet == false)
            {
                throw new Exception("Socket did not timeout.");
            }
        }

        [Fact]
        public void Client_times_out_after_server_dropped_session()
        {
            ClientConfig.PingTimeout = 200;

            Server.StateChanged += (sender, args) =>
            {
                if(args.State == TransportLayerState.Connected)
                    ((TcpTransportLayerSession)args.Session).SimulateConnectionDrop = true;

                if(args.State == TransportLayerState.Started)
                    Client.Connect();
            };


            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Closed)
                {
                    if (args.Reason == SessionCloseReason.TimeOut)
                    {
                        TestComplete.Set();
                    }
                    else
                    {
                        LastException = new Exception("Client closed for reason other than timeout.");
                    }
                }
            };

            StartAndWait(false, 1000, true, false);

            if (TestComplete.IsSet == false)
            {
                throw new Exception("Socket did not timeout.");
            }
        }



    }
}