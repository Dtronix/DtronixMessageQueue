using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public class ConnectionPerformanceTest : PerformanceTest
    {
        public ConnectionPerformanceTest() : base("Connection Test")
        {
            
        }

        public override UserControl GetConfigControl()
        {
            return null;
        }

        public override void Start()
        {
            throw new NotImplementedException();
        }
    }

    public abstract class PerformanceTest
    {
        public string Name { get; }


        protected PerformanceTest(string name)
        {
            Name = name;
        }

        public abstract UserControl GetConfigControl();
        public abstract void Start();
    }
}
