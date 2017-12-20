using System;

namespace Botje.Core
{
    /// <summary>
    /// Logging interface, undocumented because eveything here is completely obvious.
    /// </summary>
    public interface ILogger
    {
        /// <summary></summary>
        /// <param name="text"></param>
        void Trace(string text);
        /// <summary></summary>
        /// <param name="ex"></param>
        /// <param name="text"></param>
        void Trace(Exception ex, string text);
        /// <summary></summary>
        /// <param name="text"></param>
        void Info(string text);
        /// <summary></summary>
        /// <param name="ex"></param>
        /// <param name="text"></param>
        void Info(Exception ex, string text);
        /// <summary></summary>
        /// <param name="text"></param>
        void Warn(string text);
        /// <summary></summary>
        /// <param name="ex"></param>
        /// <param name="text"></param>
        void Warn(Exception ex, string text);
        /// <summary></summary>
        /// <param name="text"></param>
        void Error(string text);
        /// <summary></summary>
        /// <param name="ex"></param>
        /// <param name="text"></param>
        void Error(Exception ex, string text);

        void SetLevel(LogLevel level);
    }
}
