using System;
using System.Linq;

namespace Botje.Core.Commands
{
    /// <summary>
    /// Checks if the bot is alive.
    /// </summary>
    public class PingCommand : ConsoleCommandBase
    {
        public override CommandInfo Info => new CommandInfo
        {
            Command = "ping",
            Aliases = null,
            QuickHelp = "Pings the application to see if it's still alive",
            DetailedHelp = "Usage: ping [argument(s)]\nIf the application is alive, this pong to the console, followed by all supplied arguments (in quotes)."
        };

        public override bool OnInput(string command, string[] args)
        {
            string argstr = string.Join(", ", args.Select(x => $"\"{x}\"").ToArray());
            Console.WriteLine($"pong: {argstr}");
            return true;
        }
    }
}
