using Botje.Core;
using Botje.Core.Commands;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.PrivateConversation;
using Ninject;
using PokemonRaidBot.Entities;
using PokemonRaidBot.Modules;
using PokemonRaidBot.Utils;
using System;
using System.Linq;

namespace PokemonRaidBot.ConsoleCommands
{
    public class ListCommand : ConsoleCommandBase
    {
        private ILogger _log;
        private RaidEventHandler _eventHandler;

        [Inject]
        public IMessagingClient Client { get; set; }

        [Inject]
        public IDatabase DB { get; set; }

        [Inject]
        public IPrivateConversationManager ConversationManager { get; set; }

        [Inject]
        public ILoggerFactory LoggerFactory { set { _log = value.Create(GetType()); } }

        [Inject]
        public IBotModule[] Modules { set { _eventHandler = value.OfType<RaidEventHandler>().FirstOrDefault(); } }

        [Inject]
        public ITimeService TimeService { get; set; }

        public override CommandInfo Info => new CommandInfo
        {
            Command = "list-all",
            Aliases = new string[] { "lsa" },
            QuickHelp = "List all the finished and in-progress raids raids, most recent first",
            DetailedHelp = "Usage: list-all\nLists all the raids."
        };

        public override bool OnInput(string command, string[] args)
        {
            if (args.Length == 0 || !int.TryParse(args[0], out int num))
            {
                num = 5;
            }
            int published = 0;
            int total = 0;
            var raids = DB.GetCollection<RaidParticipation>().FindAll().OrderByDescending(x => x.Raid.RaidUnlockTime).ToList().Take(num);

            foreach (var r in raids)
            {
                total++;
                if (r.IsPublished)
                {
                    published++;
                }

                var team = DB.GetCollection<UserSettings>().Find(x => x.User.ID == r.Raid.User.ID).FirstOrDefault()?.Team;

                var isPublished = r.IsPublished ? "PUB" : "   ";
                Console.WriteLine($"[{r.PublicID}] [{isPublished }] [{published}/{total} = {Math.Round(100 * (double)published / (double)total, 1)}] [{TimeService.AsLocalShortTime(r.Raid.RaidUnlockTime)}] [{team}] [{r.Raid.User.UsernameOrName()}] [{r.Raid.Gym}]");
            }
            return true;
        }

        public override void OnStart(ILogger logger)
        {
            base.OnStart(logger);
        }
    }
}
