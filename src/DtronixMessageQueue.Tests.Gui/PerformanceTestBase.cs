using System;
using System.Runtime.InteropServices;

namespace DtronixMessageQueue.Tests.Gui
{
    public class PerformanceTestBase
    {
        private Random _rand = new Random();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);


        public static void WriteSysInfo()
        {
           /* ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
            foreach (var o in mos.Get())
            {
                var mo = (ManagementObject) o;
                Console.Write(mo["Name"]);
            }


            long memKb;
            GetPhysicallyInstalledSystemMemory(out memKb);
            Console.WriteLine(" with " + (memKb / 1024 / 1024) + " GB of RAM installed.");*/
        }


        public class ClientRunInfo
        {
            public int Runs { get; set; }
        }

        public static byte[] SequentialBytes(int len)
        {
            var number = 0;
            var val = new byte[len];

            for (var i = 0; i < len; i++)
            {
                val[i] = (byte) number++;
                if (number > 255)
                {
                    number = 0;
                }
            }
            return val;
        }

        public byte[] RandomBytes(int len)
        {
            var val = new byte[len];
            _rand.NextBytes(val);
            return val;
        }
    }
}