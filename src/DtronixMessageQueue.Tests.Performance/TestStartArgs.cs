using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Tests.Performance
{
    public class TestStartArgs
    {
        public StartMode Mode { get; set; }



        public enum StartMode 
        {
            ServerRepeat,
            ServerRespond,
            Client
        }
    }
}
