using System;
using System.Linq;

namespace Botje.Core.Commands
{
    /// <summary>
    /// Change the loglevel.
    /// </summary>
    public class LogLevelCommand : ConsoleCommandBase
    {
        public override CommandInfo Info => new CommandInfo
        {
            Command = "loglevel",
            Aliases = new string[] { "ll" },
            QuickHelp = "Set the log level all, trace, info, warn, error or none",
            DetailedHelp = "Usage: loglevel <all|trace|info|warn|error|none>\nUpdates the loglevel."
        };

        public override bool OnInput(string command, string[] args)
        {
            if (args.Length != 1) return false;
            var newLevel = Enum.GetValues(typeof(LogLevel)).OfType<LogLevel>().Select(x => $"{x}".ToLowerInvariant()).ToList().IndexOf(args[0]);
            if (newLevel > (int)LogLevel.Error || newLevel < (int)LogLevel.All)
            {
                Console.WriteLine($"Invalid value. Valid values are {string.Join(", ", Enum.GetValues(typeof(LogLevel)).OfType<LogLevel>().Select(x => $"{x}".ToLowerInvariant()).ToList())}");
                return true;
            }

            _logger.SetLevel((LogLevel)newLevel);
            return true;
        }
    }
}

