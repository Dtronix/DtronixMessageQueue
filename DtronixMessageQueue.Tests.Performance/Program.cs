using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
			switch (mode) {
				case "mq":
					Console.WriteLine("Running MQ performance tests.\r\n");
					new MqPerformanceTest(args);
					break;

				default:
					Console.WriteLine("Running RPC performance tests.\r\n");
					new RpcPerformanceTest(args);
					break;
			}

			Console.ReadLine();

		}
		

	}

}
