using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Logging events.
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// Level of the logged event.
        /// </summary>
        public LogEventLevel Level { get; set; }

        /// <summary>
        /// Message from logging source.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Time the log occurred.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Filename of the logged event.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Name of the member which logged this event.
        /// </summary>
        public string MemberName { get; set; }

        /// <summary>
        /// Line number where the event was logged.
        /// </summary>
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
