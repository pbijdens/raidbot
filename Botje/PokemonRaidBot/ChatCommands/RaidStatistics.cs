using Botje.Core.Utils;
using Botje.Messaging.Models;
using Ninject;
using PokemonRaidBot.Modules;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PokemonRaidBot.ChatCommands
{
    /// <summary>
    /// 
    /// </summary>
    public class RaidStatistics : ChatCommandModuleBase
    {
        private RaidEventHandler _eventHandler;

        [Inject]
        public IBotModule[] Modules { set { _eventHandler = value.OfType<RaidEventHandler>().FirstOrDefault(); } }

        public override void ProcessCommand(Source source, Message message, string command, string[] args)
        {
            switch (command)
            {
                case "/gyminfo":
                    GymInfo(message, args);
                    break;
                default:
                    return;
            }
        }

        private void GymInfo(Message message, string[] args)
        {
            string argstr = args[0];

            Regex re = null;
            if (string.IsNullOrWhiteSpace(argstr))
            {
                Client.SendMessageToChat(message.Chat.ID, $"Gebruik /gyminfo \"<regex voor naam van de gym>\"", "HTML", true, false, message.MessageID);
                return;
            }

            int sent = 0;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Statistieken voor \"{argstr}\"");
                re = new Regex(argstr, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                var raids = DB.GetCollection<Entities.RaidParticipation>().Find(x => re.IsMatch(x.Raid.Gym)).OrderBy(x => x.Raid.RaidEndTime);
                foreach (var raid in raids)
                {
                    int total = raid.NumberOfParticipants();
                    string line = $"\"{total}\",\"{TimeUtils.AsLocalShortTime(raid.Raid.RaidEndTime)}\",\"{MessageUtils.HtmlEscape(raid.Raid.Gym)}\"";
                    if (sb.ToString().Length + line.Length > 4094)
                    {
                        Client.SendMessageToChat(message.Chat.ID, sb.ToString(), "HTML", true, false, message.MessageID);
                        sb.Clear();
                        sent++;
                    }
                    if (sent >= 10) // er zijn grenzen
                    {
                        Client.SendMessageToChat(message.Chat.ID, "Te veel resultaten.", "HTML", true, false, message.MessageID);
                        return;
                    }
                    sb.AppendLine(line);
                }
                Client.SendMessageToChat(message.Chat.ID, sb.ToString(), "HTML", true, false, message.MessageID);
            }
            catch (Exception ex)
            {
                Client.SendMessageToChat(message.Chat.ID, $"Dat gaat niet goed, is \"{_(argstr)}\" wel een regular expression: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}", "HTML", true, false, message.MessageID);
            }
        }
    }
}
