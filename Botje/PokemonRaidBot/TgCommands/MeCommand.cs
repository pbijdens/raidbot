using Botje.Core.Commands;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.Models;
using Ninject;
using System;

namespace PokemonRaidBot.TgCommands
{
    /// <summary>
    /// WHO AM I?!?!?!
    /// </summary>
    public class MeCommand : ConsoleCommandBase
    {
        [Inject]
        public IMessagingClient Client { get; set; }

        [Inject]
        public IDatabase DB { get; set; }

        public override CommandInfo Info => new CommandInfo
        {
            Command = "who",
            Aliases = new string[] { "whoami", "me" },
            QuickHelp = "Who am i",
            DetailedHelp = "Usage: who [am [i]]\nReturns the bot's identity."
        };

        /// <summary>
        /// Check with the telegrm client to see who I am.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public override bool OnInput(string command, string[] args)
        {
            User me = Client.GetMe();
            Console.WriteLine($"I am:");
            Console.WriteLine($"- ID: \"{me.ID}\"");
            Console.WriteLine($"- Username: \"{me.Username}\"");
            Console.WriteLine($"- First name: \"{me.FirstName}\"");
            Console.WriteLine($"- Last name: \"{me.LastName}\"");
            Console.WriteLine($"- Is a bot: \"{me.IsBot}\"");
            Console.WriteLine($"- Language code: \"{me.LanguageCode}\"");
            return true;
        }
    }
}
