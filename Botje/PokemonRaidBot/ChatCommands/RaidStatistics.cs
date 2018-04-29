using Botje.Core.Utils;
using Botje.Messaging.Models;
using Ninject;
using PokemonRaidBot.Modules;
using PokemonRaidBot.Utils;
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

        [Inject]
        public ITimeService TimeService { get; set; }

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
            string argstr = args.Length == 1 ? args[0] : "";

            Regex re = null;
            if (string.IsNullOrWhiteSpace(argstr))
            {
                Client.SendMessageToChat(message.Chat.ID, _HTML_(I18N.GetString($"Use '/gyminfo \"<regex>\"' to obtain statistics about all raids on matching gyms.")), "HTML", true, false, message.MessageID);
                return;
            }

            int sent = 0;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(I18N.GetString($"Statistics for \"{0}\"", argstr));
                re = new Regex(argstr, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                var raids = DB.GetCollection<Entities.RaidParticipation>().Find(x => re.IsMatch(x.Raid.Gym)).OrderBy(x => x.Raid.RaidEndTime);
                foreach (var raid in raids)
                {
                    int total = raid.NumberOfParticipants();
                    string line = $"\"{total}\",\"{TimeService.AsLocalShortTime(raid.Raid.RaidEndTime)}\",\"{MessageUtils.HtmlEscape(raid.Raid.Gym)}\"";
                    if (sb.ToString().Length + line.Length > 4094)
                    {
                        Client.SendMessageToChat(message.Chat.ID, sb.ToString(), "HTML", true, false, message.MessageID);
                        sb.Clear();
                        sent++;
                    }
                    if (sent >= 10) // er zijn grenzen
                    {
                        Client.SendMessageToChat(message.Chat.ID, I18N.GetString("Too many results."), "HTML", true, false, message.MessageID);
                        return;
                    }
                    sb.AppendLine(line);
                }
                Client.SendMessageToChat(message.Chat.ID, sb.ToString(), "HTML", true, false, message.MessageID);
            }
            catch (Exception ex)
            {
                string msg = I18N.GetString("An error occurred processing statistics for \"{0}\" (is that even a regular expression?): {1}", _HTML_(argstr), _HTML_(ExceptionUtils.AsString(ex)));
                Client.SendMessageToChat(message.Chat.ID, msg, "HTML", true, false, message.MessageID);
            }
        }
    }
}
