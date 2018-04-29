using Botje.Core;
using Botje.Core.Commands;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.PrivateConversation;
using Ninject;
using PokemonRaidBot.Entities;
using PokemonRaidBot.Modules;
using System;
using System.Linq;

namespace PokemonRaidBot.ConsoleCommands
{
    public class UpdateCommand : ConsoleCommandBase
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

        public override CommandInfo Info => new CommandInfo
        {
            Command = "update",
            Aliases = new string[] { "u" },
            QuickHelp = "Update a raid",
            DetailedHelp = "Usage: update <what> <value>\r\n" +
            "\r\n" +
            "Examples\r\n" +
            "- update xjsdjdiUYSi44d5hljr time 2018-04-07T11:00:00Z\r\n"
        };

        public override bool OnInput(string command, string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Seek help.");
                return true;
            }
            var raid = DB.GetCollection<RaidParticipation>().Find(r => r.PublicID == args[0]).SingleOrDefault();
            if (null == raid)
            {
                Console.Write("No such raid.");
                return true;
            }

            switch (args[1])
            {
                case "time":
                    {
                        if (args.Length != 3)
                        {
                            Console.WriteLine("Seek help.");
                            return true;
                        }
                        if (!DateTimeOffset.TryParse(args[2], out DateTimeOffset dt))
                        {
                            Console.WriteLine("The is not a valid ISO timestamp, those look like 2018-04-01T13:00:00Z");
                            return true;
                        }
                        var oldUnlockTime = raid.Raid.RaidUnlockTime;
                        var oldEndTime = raid.Raid.RaidEndTime;
                        raid.Raid.RaidUnlockTime = dt.UtcDateTime;
                        raid.Raid.RaidEndTime = raid.Raid.RaidUnlockTime + TimeSpan.FromMinutes(RaidCreationWizard.RaidDurationInMinutes);
                        foreach (var x in raid.Participants)
                        {
                            foreach (var participant in x.Value)
                            {
                                participant.UtcWhen += raid.Raid.RaidUnlockTime - oldUnlockTime;
                            }
                        }
                        DB.GetCollection<RaidParticipation>().Update(raid);
                        Console.Write("Done.");
                    }
                    break;
            }
            return true;
        }

        public override void OnStart(ILogger logger)
        {
            base.OnStart(logger);
        }
    }
}
