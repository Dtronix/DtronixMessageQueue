using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue
{
    public class LogEventArgs : EventArgs
    {
        public LogEventLevel Level { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }
        public string Filename { get; set; }
        public string MemberName { get; set; }
        public int SourceLineNumber { get; set; }

        public LogEventArgs(LogEventLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        public LogEventArgs()
        {
            
        }
    }
}
