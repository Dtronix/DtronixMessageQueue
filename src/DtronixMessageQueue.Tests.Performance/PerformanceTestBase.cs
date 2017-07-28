using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Tests.Performance
{
    abstract class PerformanceTestBase
    {

        private Random rand = new Random();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long total_memory_in_kilobytes);


        public static void WriteSysInfo()
        {
            GetSysInfo(out var memory, out var filename, out var fullProcessor);

            Console.Write(fullProcessor);
            Console.WriteLine(" with " + (memory / 1024 / 1024) + " GB of RAM installed.");
        }

        public static void GetSysInfo(out long memory, out string filename, out string fullProcessor)
        {
            ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
            fullProcessor = "";
            foreach (var o in mos.Get())
            {
                var mo = (ManagementObject)o;
                fullProcessor = mo["Name"].ToString();
            }


            GetPhysicallyInstalledSystemMemory(out memory);

            var processorParser = new Regex("(i[0-9]-[0-9]*.*?) ");
            var matches = processorParser.Match(fullProcessor);

            if (!matches.Success)
                filename = $"MQTest-{memory / 1024 / 1024}";
            else
                filename = $"{matches.Groups[1].Value}-{memory / 1024 / 1024}GB.md";
        }


        public class ClientRunInfo
        {
            public int Runs { get; set; }
            public SimpleMqSession Session { get; set; }

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
            rand.NextBytes(val);
            return val;
        }

        public abstract void StartTest();
    }
}
