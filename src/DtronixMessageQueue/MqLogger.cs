using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Basic framework for attaching a full logging system into the Message Queue logging.
    /// </summary>
    public class MqLogger
    {
        /// <summary>
        /// Event called when an event is logged.
        /// </summary>
        public EventHandler<LogEventArgs> LogEvent;

        /// <summary>
        /// Minimum level of logging.  Discards all lower level log events.
        /// </summary>
        public LogEventLevel MinimumLogLevel { get; set; } = LogEventLevel.Error;

        /// <summary>
        /// Logs a trace event.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="memberName">Name of the calling member.</param>
        /// <param name="sourceFilePath">Path of the calling class/file.</param>
        /// <param name="sourceLineNumber">Line number this method is called from.</param>
        public virtual void Trace(string message, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "", 
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Trace, memberName, sourceFilePath, sourceLineNumber);
        }


        /// <summary>
        /// Logs a debug event.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="memberName">Name of the calling member.</param>
        /// <param name="sourceFilePath">Path of the calling class/file.</param>
        /// <param name="sourceLineNumber">Line number this method is called from.</param>
        public virtual void Debug(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Debug, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Logs an info event.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="memberName">Name of the calling member.</param>
        /// <param name="sourceFilePath">Path of the calling class/file.</param>
        /// <param name="sourceLineNumber">Line number this method is called from.</param>
        public virtual void Info(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Info, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Logs a warning event.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="memberName">Name of the calling member.</param>
        /// <param name="sourceFilePath">Path of the calling class/file.</param>
        /// <param name="sourceLineNumber">Line number this method is called from.</param>
        public virtual void Warn(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Warn, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Logs an error event.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="memberName">Name of the calling member.</param>
        /// <param name="sourceFilePath">Path of the calling class/file.</param>
        /// <param name="sourceLineNumber">Line number this method is called from.</param>
        public virtual void Error(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Error, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Logs a fatal event.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="memberName">Name of the calling member.</param>
        /// <param name="sourceFilePath">Path of the calling class/file.</param>
        /// <param name="sourceLineNumber">Line number this method is called from.</param>
        public virtual void Fatal(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(message, LogEventLevel.Fatal, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Logs a event with the specified level.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="logLevel">Level of this event to log.</param>
        /// <param name="memberName">Name of the calling member.</param>
        /// <param name="sourceFilePath">Path of the calling class/file.</param>
        /// <param name="sourceLineNumber">Line number this method is called from.</param>
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