using Ninject;
using System;
using System.Linq;

namespace Botje.Core.Commands
{
    /// <summary>
    /// When a user requests help...
    /// </summary>
    public class HelpCommand : ConsoleCommandBase
    {
        [Inject]
        public IConsoleCommand[] Commands { get; set; }

        public override CommandInfo Info => new CommandInfo
        {
            Command = "help",
            Aliases = new string[] { "h", "?" },
            QuickHelp = "Request help",
            DetailedHelp = "Usage: help [command]\nWhen no command is pecified, shows a quick overview of all commands. When a command is specified explains the usage of that command."
        };

        public override bool OnInput(string command, string[] args)
        {
            if (args.Length >= 1)
            {
                foreach (var commandObj in Commands)
                {
                    if (string.Equals(commandObj.Info.Command, args[0], StringComparison.InvariantCultureIgnoreCase) ||
                                                    (commandObj.Info.Aliases != null && commandObj.Info.Aliases.Where(x => string.Equals(x, args[0], StringComparison.InvariantCultureIgnoreCase)).Any()))
                    {
                        Console.WriteLine($"Help on: {commandObj.Info.Command}");
                        string aliasstr = commandObj.Info.Aliases == null ? "" : string.Join(", ", commandObj.Info.Aliases);
                        Console.WriteLine($"Aliases: {aliasstr}");
                        Console.WriteLine($"Description:");
                        Console.WriteLine(commandObj.Info.DetailedHelp);
                        Console.WriteLine($"---");
                    }
                }
            }
            else
            {
                var maxlen = Commands.Select(x => x.Info.Command.Length).Max();
                foreach (var c in Commands.OrderBy(x => x.Info.Command))
                {
                    Console.WriteLine($"{string.Format($"{{0,-{maxlen}}}", c.Info.Command)} - {c.Info.QuickHelp}");
                }
            }
            return true;
        }
    }
}
