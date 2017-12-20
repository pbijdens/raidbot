namespace Botje.Core
{
    public enum LogLevel : int
    {
        All = 0,
        Trace,
        Info,
        Warn,
        Error,
        None = int.MaxValue
    }
}
