using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Tests.Performance {
	class PerformanceTestBase {

		private Random rand = new Random();

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetPhysicallyInstalledSystemMemory(out long total_memory_in_kilobytes);


		public static void WriteSysInfo() {
			ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
			foreach (var o in mos.Get()) {
				var mo = (ManagementObject)o;
				Console.Write(mo["Name"]);
			}


			long mem_kb;
			GetPhysicallyInstalledSystemMemory(out mem_kb);
			Console.WriteLine(" with " + (mem_kb / 1024 / 1024) + " GB of RAM installed.");
		}


		public class ClientRunInfo {
			public int Runs { get; set; }
			public SimpleMqSession Session { get; set; }

		}

		public static byte[] SequentialBytes(int len) {
			var number = 0;
			var val = new byte[len];

			for (var i = 0; i < len; i++) {
				val[i] = (byte)number++;
				if (number > 255) {
					number = 0;
				}
			}
			return val;
		}

		public byte[] RandomBytes(int len) {
			var val = new byte[len];
			rand.NextBytes(val);
			return val;
		}
	}
}
