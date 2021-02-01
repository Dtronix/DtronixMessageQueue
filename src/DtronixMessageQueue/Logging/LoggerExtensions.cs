using System;

namespace DtronixMessageQueue.Logging
{
    public static class LoggerExtensions
    {
        // Trace
        public static void Trace(this ILogger logger, Exception exception)
        {
            logger.Log(new LogEntry(LogEntryEventType.Trace, exception.Message, exception));
        }

        public static void Trace(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Trace, message));
        }

        public static void Trace(this ILogger logger, Exception exception, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Trace, message, exception));
        }

        // Debug
        public static void Debug(this ILogger logger, Exception exception)
        {
            logger.Log(new LogEntry(LogEntryEventType.Debug, exception.Message, exception));
        }

        public static void Debug(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Debug, message));
        }

        public static void Debug(this ILogger logger, Exception exception, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Debug, message, exception));
        }

        // Info
        public static void Info(this ILogger logger, Exception exception)
        {
            logger.Log(new LogEntry(LogEntryEventType.Info, exception.Message, exception));
        }

        public static void Info(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Info, message));
        }

        public static void Info(this ILogger logger, Exception exception, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Info, message, exception));
        }

        // Warn
        public static void Warn(this ILogger logger, Exception exception)
        {
            logger.Log(new LogEntry(LogEntryEventType.Warn, exception.Message, exception));
        }

        public static void Warn(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Warn, message));
        }

        public static void Warn(this ILogger logger, Exception exception, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Warn, message, exception));
        }

        // Error
        public static void Error(this ILogger logger, Exception exception)
        {
            logger.Log(new LogEntry(LogEntryEventType.Error, exception.Message, exception));
        }

        public static void Error(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Error, message));
        }

        public static void Error(this ILogger logger, Exception exception, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Error, message, exception));
        }

        // Fatal
        public static void Fatal(this ILogger logger, Exception exception)
        {
            logger.Log(new LogEntry(LogEntryEventType.Fatal, exception.Message, exception));
        }

        public static void Fatal(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Fatal, message));
        }

        public static void Fatal(this ILogger logger, Exception exception, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Fatal, message, exception));
        }

        [Conditional("DEBUG")]
        public static void ConditionalDebug(this ILogger logger, Exception exception)
        {
            logger.Log(new LogEntry(LogEntryEventType.Debug, exception.Message, exception));
        }

        [Conditional("DEBUG")]
        public static void ConditionalDebug(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Debug, message));
        }

        [Conditional("DEBUG")]
        public static void ConditionalDebug(this ILogger logger, string message, Exception exception)
        {
            logger.Log(new LogEntry(LogEntryEventType.Debug, message, exception));
        }

        [Conditional("DEBUG")]
        public static void ConditionalTrace(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LogEntryEventType.Trace, message));
        }

        [Conditional("DEBUG")]
        public static void ConditionalTrace(this ILogger logger, Exception exception)
        {
            logger.Log(new LogEntry(LogEntryEventType.Trace, exception.Message, exception));
        }

        [Conditional("DEBUG")]
        public static void ConditionalTrace(this ILogger logger, string message, Exception exception)
        {
            logger.Log(new LogEntry(LogEntryEventType.Trace, message, exception));
        }
    }
}