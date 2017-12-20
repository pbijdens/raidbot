using System;

namespace Botje.Core.Loggers
{
    /// <summary>
    /// Factory for the console logger.
    /// </summary>
    public class ConsoleLoggerFactory : ILoggerFactory
    {
        public ILogger Create(Type t)
        {
            return new ConsoleLogger()
            {
                Class = t.Name
            };
        }
    }
}
