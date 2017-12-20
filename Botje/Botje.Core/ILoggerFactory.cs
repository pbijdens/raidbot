using System;

namespace Botje.Core
{
    public interface ILoggerFactory
    {
        /// <summary>
        /// Create a new logger for this type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        ILogger Create(Type t);
    }
}
