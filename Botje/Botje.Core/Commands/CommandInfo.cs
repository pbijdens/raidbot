using System;

namespace Botje.Core.Commands
{
    /// <summary>
    /// Command information
    /// </summary>
    public class CommandInfo
    {
        /// <summary>
        /// Primary name of the command.
        /// </summary>
        public String Command;
        /// <summary>
        /// Aliases for the command.
        /// </summary>
        public string[] Aliases = new string[] { };
        /// <summary>
        /// Shown in the 'help' overview.
        /// </summary>
        public string QuickHelp = "";
        /// <summary>
        /// Shown when asking help for this command.
        /// </summary>
        public string DetailedHelp = "No help (yet)";
    }
}
