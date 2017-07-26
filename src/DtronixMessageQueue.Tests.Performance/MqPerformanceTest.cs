using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Tests.Performance
{
    class MqPerformanceTest : PerformanceTestBase
    {

        private readonly MqConfig _config;
        private readonly MqMessage _smallMessage;
        private readonly MqMessage _medimumMessage;
        private readonly MqMessage _largeMessage;
        private Stopwatch _sw;
        private Semaphore _loopSemaphore;
        private int count;
        private double[] totalValues;
        private Semaphore _testCompleteSemaphore;
        private MqServer<SimpleMqSession, MqConfig> _server;
        private MqClient<SimpleMqSession, MqConfig> _client;

        public MqPerformanceTest()
        {

            _config = new MqConfig
            {
                Ip = "127.0.0.1",
                Port = 2828
            };

            _smallMessage = new MqMessage
            {
                new MqFrame(SequentialBytes(50), MqFrameType.More, _config),
                new MqFrame(SequentialBytes(50), MqFrameType.More, _config),
                new MqFrame(SequentialBytes(50), MqFrameType.More, _config),
                new MqFrame(SequentialBytes(50), MqFrameType.Last, _config)
            };

            _medimumMessage = new MqMessage
            {
                new MqFrame(SequentialBytes(500), MqFrameType.More, _config),
                new MqFrame(SequentialBytes(500), MqFrameType.More, _config),
                new MqFrame(SequentialBytes(500), MqFrameType.More, _config),
                new MqFrame(SequentialBytes(500), MqFrameType.Last, _config)
            };

            _largeMessage = new MqMessage();

            for (int i = 0; i < 20; i++)
            {
                _largeMessage.Add(new MqFrame(SequentialBytes(3000), MqFrameType.More, _config));
            }

            _sw = new Stopwatch();
            _loopSemaphore = new Semaphore(0, 1);
            _testCompleteSemaphore = new Semaphore(0, 1);

            count = 0;
        }


        public override void StartTest()
        {

            Console.WriteLine("FrameBufferSize: {0}; SendAndReceiveBufferSize: {1}\r\n", _config.FrameBufferSize,
                _config.SendAndReceiveBufferSize);


            MqInProcessPerformanceTests(1000000, 5, _smallMessage, _config);

            MqInProcessPerformanceTests(100000, 5, _medimumMessage, _config);


            MqInProcessPerformanceTests(10000, 5, _largeMessage, _config);
        }

        private void MqInProcessPerformanceTests(int totalMessages, int loops, MqMessage message, MqConfig config)
        {
            _server = new MqServer<SimpleMqSession, MqConfig>(config);
            _server.Start();

            totalValues = new[] {0d, 0d, 0d};


            _client = new MqClient<SimpleMqSession, MqConfig>(config);

            Console.WriteLine("|   Build |   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |");
            Console.WriteLine("|---------|------------|-----------|--------------|------------|----------|");



            _server.IncomingMessage += (sender, args2) =>
            {
                count += args2.Messages.Count;


                if (count == totalMessages)
                {
                    ReportResults(totalMessages, _sw.ElapsedMilliseconds, message.Size);
                    _loopSemaphore.Release();

                }
            };



            _client.Connected += (sender, args) =>
            {
                for (var i = 0; i < loops; i++)
                {
                    SendMessages(args.Session, message, totalMessages, 5000);
                }

                Console.WriteLine("|         |            |  AVERAGES | {0,12:N0} | {1,10:N0} | {2,8:N2} |",
                    totalValues[0] / loops,
                    totalValues[1] / loops, totalValues[2] / loops);
                Console.WriteLine();

                _server.Stop();
                _client.Close();

                _testCompleteSemaphore.Release();

            };

            _client.Connect();

            _testCompleteSemaphore.WaitOne(30000);


        }

        public void SendMessages(SimpleMqSession client, MqMessage message, int totalMessages, int timeout)
        {

            count = 0;
            _sw.Restart();

            for (var i = 0; i < totalMessages; i++)
            {
                client.Send(message);
            }

            if (!_loopSemaphore.WaitOne(timeout))
            {
                Console.WriteLine($"Test failed to complete with {count} of {totalMessages} loops performed.");

                ReportResults(totalMessages, _sw.ElapsedMilliseconds, message.Size);
            }



        }

        private void ReportResults(int totalMessages, long milliseconds, int messageSize)
        {
            var mode = "Release";

#if DEBUG
            mode = "Debug";
#endif

            var messagesPerSecond = (int) ((double) totalMessages / milliseconds * 1000);
            var msgSizeNoHeader = messageSize - 12;
            var mbps = totalMessages * (double) (msgSizeNoHeader) / milliseconds / 1000;
            Console.WriteLine("| {0,7} | {1,10:N0} | {2,9:N0} | {3,12:N0} | {4,10:N0} | {5,8:N2} |", mode, totalMessages,
                msgSizeNoHeader, milliseconds, messagesPerSecond, mbps);
            totalValues[0] += milliseconds;
            totalValues[1] += messagesPerSecond;
            totalValues[2] += mbps;
        }
    }

}
