using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace DtronixMessageQueue.Tests.Performance {
	class Program {

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetPhysicallyInstalledSystemMemory(out long total_memory_in_kilobytes);

		static void Main(string[] args) {
			var mode = args.Length == 0 ? null : args[0];
			var file_name = string.Join("-", args);
			using (var cc = new ConsoleCopy($"MessageQueuePerformanceTest-{file_name}.txt")) {
				PerformanceTestBase.WriteSysInfo();

				Console.WriteLine($"DMQPerf.exe {string.Join(" ", args)}");

                Console.WriteLine("MQ Performance tests.\r\n");
                new MqPerformanceTest().StartTest();

                Console.ReadLine();

                Console.WriteLine("RPC Performance tests.\r\n");
                new RpcPerformanceTest(args);
			}

			Console.ReadLine();

		}
		

	}

}
