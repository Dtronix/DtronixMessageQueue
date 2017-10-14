﻿using System;
using System.Threading;
using DtronixMessageQueue.Tests.Mq;
using DtronixMessageQueue.TransportLayer;
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
                    args.Session.Close(SessionCloseReason.Closing, false);
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
                    args.Session.Close(SessionCloseReason.Closing, false);

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
                    args.Session.Close(SessionCloseReason.Closing, false);
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
    }
}