using Botje.Core;
using Botje.Core.Commands;
using System;
using System.Threading;

namespace PokemonRaidBot.ConsoleCommands
{
    public class ExitCommand : ConsoleCommandBase
    {
        public CancellationTokenSource TokenSource { get; set; }

        public override CommandInfo Info => new CommandInfo
        {
            Command = "exit",
            Aliases = new string[] { "quit", "shutdown" },
            QuickHelp = "Terminate the bot",
            DetailedHelp = "Usage: exit\nStops the bot and terminates all running processes cleanly."
        };

        public override bool OnInput(string command, string[] args)
        {
            TokenSource.Cancel();
            return true;
        }

        public override void OnStart(ILogger logger)
        {
            Console.WriteLine($"Starting the bot. Server time is {DateTimeOffset.Now}.");

            base.OnStart(logger);
        }
    }
}
