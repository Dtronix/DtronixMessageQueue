using System;

namespace DtronixMessageQueue.Logging
{
    public class LogEntry
    {
        public readonly LogEntryEventType Severity;
        public readonly string Message;
        public readonly Exception Exception;

        public LogEntry(LogEntryEventType severity, string message, Exception exception = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message == string.Empty)
                throw new ArgumentException("empty", nameof(message));

            Severity = severity;
            Message = message;
            Exception = exception;
        }
    }
}