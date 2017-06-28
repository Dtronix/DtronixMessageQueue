﻿using System;
using System.Runtime.InteropServices;

namespace DtronixMessageQueue.Tests.Performance
{
    class Program
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);

        static void Main(string[] args)
        {

            var mode = args.Length == 0 ? null : args[0];
            var fileName = string.Join("-", args);
            using (var cc = new ConsoleCopy($"MessageQueuePerformanceTest-{fileName}.txt"))
            {
                //PerformanceTestBase.WriteSysInfo();

                //Console.WriteLine($"DMQPerf.exe {string.Join(" ", args)}");

                switch (mode)
                {
                    case "mq-throughput":
                        Console.WriteLine("Running MQ performance tests.\r\n");
                        new MqThroughputTest(args);
                        break;

                    default:
                        Console.WriteLine("Running MQ performance tests.\r\n");
                        new MqThroughputTest(args);
                        break;
                }
            }

            Console.ReadLine();
        }
    }
}