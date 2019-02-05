using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue
{
    public class MqLogger
    {
        /// <summary>
        /// Event called when an event is logged.
        /// </summary>
        public EventHandler<LogEventArgs> LogEvent;

        public LogEventLevel MinimumLogLevel { get; set; } = LogEventLevel.Error;


        public virtual void Trace(string message, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "", 
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Trace, memberName, sourceFilePath, sourceLineNumber);
        }


        public virtual void Debug(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Debug, memberName, sourceFilePath, sourceLineNumber);
        }

        public virtual void Info(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Info, memberName, sourceFilePath, sourceLineNumber);
        }

        public virtual void Warn(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Warn, memberName, sourceFilePath, sourceLineNumber);
        }

        public virtual void Error(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Error, memberName, sourceFilePath, sourceLineNumber);
        }

        public virtual void Fatal(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Fatal, memberName, sourceFilePath, sourceLineNumber);
        }

        public void Log(string message,
            LogEventLevel logLevel,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (logLevel >= MinimumLogLevel)
                LogEvent?.Invoke(this, new LogEventArgs(logLevel, message)
                {
                    Time = DateTime.Now,
                    Filename = Path.GetFileName(sourceFilePath),
                    SourceLineNumber = sourceLineNumber,
                    MemberName = memberName
                });
        }
    }
}