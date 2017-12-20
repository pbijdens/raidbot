using Botje.Core.Utils;
using System;

namespace Botje.Core.Loggers
{
    /// <summary>
    /// Logs to the debug console.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public string Class { get; set; }

        public void Error(string text)
        {
            if (ConsoleLogLevel > LogLevel.Error) return;
            Console.WriteLine($"{DateTime.Now} ERROR ({Class}): {text}");
        }

        public void Error(Exception ex, string text)
        {
            if (ConsoleLogLevel > LogLevel.Error) return;
            Console.WriteLine($"{DateTime.Now} ERROR ({Class}): {text}\r\n{ExceptionUtils.AsString(ex)}");
        }

        public void Info(string text)
        {
            if (ConsoleLogLevel > LogLevel.Info) return;
            Console.WriteLine($"{DateTime.Now} INFO ({Class}): {text}");
        }

        public void Info(Exception ex, string text)
        {
            if (ConsoleLogLevel > LogLevel.Info) return;
            Console.WriteLine($"{DateTime.Now} INFO ({Class}): {text}\r\n{ExceptionUtils.AsString(ex)}");
        }

        public void Trace(string text)
        {
            if (ConsoleLogLevel > LogLevel.Trace) return;
            Console.WriteLine($"{DateTime.Now} TRACE ({Class}): {text}");
        }

        public void Trace(Exception ex, string text)
        {
            if (ConsoleLogLevel > LogLevel.Trace) return;
            Console.WriteLine($"{DateTime.Now} TRACE ({Class}): {text}\r\n{ExceptionUtils.AsString(ex)}");
        }

        public void Warn(string text)
        {
            if (ConsoleLogLevel > LogLevel.Warn) return;
            Console.WriteLine($"{DateTime.Now} WARN ({Class}): {text}");
        }

        public void Warn(Exception ex, string text)
        {
            if (ConsoleLogLevel > LogLevel.Warn) return;
            Console.WriteLine($"{DateTime.Now} WARN ({Class}): {text}\r\n{ExceptionUtils.AsString(ex)}");
        }

        public static LogLevel ConsoleLogLevel = LogLevel.Info;
        public void SetLevel(LogLevel level)
        {
            ConsoleLogLevel = level;
        }
    }
}
