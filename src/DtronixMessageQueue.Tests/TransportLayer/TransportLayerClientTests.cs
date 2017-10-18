using System;
using System.Threading;
using DtronixMessageQueue.Tests.Mq;
using DtronixMessageQueue.TransportLayer;
using DtronixMessageQueue.TransportLayer.Tcp;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.TransportLayer
{
    public class TransportLayerClientTests : TransportLayerTestsBase
    {

        [Test]
        public void Client_connects_to_server()
        {
            Client.StateChanged += (sender, args) =>
            {
                if(args.State == TransportLayerState.Connected)
                    TestComplete.Set();
            };

            StartAndWait();
        }

        [Test]
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

        [Test]
        public void Client_notifies_server_of_close()
        {
            Client.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.Close(SessionCloseReason.Closing);
            };


            Server.Received += (sender, args) =>
            {
                if(args.Buffer == null)
                    TestComplete.Set();
            };

            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.ReceiveAsync();
            };

            StartAndWait();
        }

        [Test]
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
                    Assert.AreEqual(randomBytes, args.Buffer);
                    TestComplete.Set();
                }
                catch (Exception e)
                {
                    LastException = e;
                }
            };

            Server.StateChanged += (sender, args) =>
            {
                if (args.State == TransportLayerState.Connected)
                    args.Session.ReceiveAsync();
            };

            StartAndWait();
        }

        [Test]
        public void Client_times_out_on_long_connection()
        {
            ClientConfig.ConnectionTimeout = 200;

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
    }
}