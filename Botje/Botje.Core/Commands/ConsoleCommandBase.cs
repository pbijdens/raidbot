namespace Botje.Core.Commands
{
    /// <summary>
    /// Although console commands only have to implement the IConsoleCommand interface, using this base class helps.
    /// </summary>
    public abstract class ConsoleCommandBase : IConsoleCommand
    {
        /// <summary>
        /// Logger
        /// </summary>
        protected ILogger _logger;

        /// <summary>
        /// The command-info
        /// </summary>
        public abstract CommandInfo Info { get; }

        /// <summary>
        /// May provess a command.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// <returns>true if the command was fully processed and should not be offered to any other commands</returns>
        public abstract bool OnInput(string command, string[] args);

        /// <summary>
        /// Starting? Set the logger.
        /// </summary>
        /// <param name="logger"></param>
        public virtual void OnStart(ILogger logger)
        {
            logger.Trace($"Registered: {GetType().Name}");
            _logger = logger;
        }
    }
}
