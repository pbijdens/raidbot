namespace Botje.Core.Commands
{
    public interface IConsoleCommand
    {
        CommandInfo Info { get; }

        void OnStart(ILogger logger);

        bool OnInput(string command, string[] args);
    }
}
