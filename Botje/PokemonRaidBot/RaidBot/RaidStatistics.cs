using Botje.Core;
using Botje.Core.Utils;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.Models;
using Botje.Messaging.PrivateConversation;
using Ninject;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PokemonRaidBot.RaidBot
{
    /// <summary>
    /// 
    /// </summary>
    public class RaidStatistics : IBotModule
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

        public void Startup()
        {
            Client.OnPublicMessage += Client_OnPublicMessage;
            Client.OnPrivateMessage += Client_OnPrivateMessage;
            _log.Trace($"Started {GetType().Name}");
        }

        public void Shutdown()
        {
            Client.OnPrivateMessage -= Client_OnPrivateMessage;
            Client.OnPublicMessage -= Client_OnPublicMessage;
            _log.Trace($"Shut down {GetType().Name}");
        }

        private void Client_OnPrivateMessage(object sender, Botje.Messaging.Events.PrivateMessageEventArgs e)
        {
            string command = GetCommandFromMessage(e.Message, out string argstr, out string[] args);
            ProcessCommand(e.Message, command, argstr, args);
        }

        private void Client_OnPublicMessage(object sender, Botje.Messaging.Events.PublicMessageEventArgs e)
        {
            string command = GetCommandFromMessage(e.Message, out string argstr, out string[] args);
            ProcessCommand(e.Message, command, argstr, args);
        }

        private void ProcessCommand(Message message, string command, string argstr, string[] args)
        {
            switch (command ?? "")
            {
                case "/gyminfo":
                    GymInfo(message, argstr, args);
                    break;
                default:
                    return;
            }
        }

        private void GymInfo(Message message, string argstr, string[] args)
        {
            Regex re = null;
            if (string.IsNullOrWhiteSpace(argstr))
            {
                Client.SendMessageToChat(message.Chat.ID, $"Gebruik /gyminfo <expressie voor naam van de gym>", "HTML", true, false, message.MessageID);
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
                string expr = MessageUtils.HtmlEscape(argstr ?? "");
                Client.SendMessageToChat(message.Chat.ID, $"Dat gaat niet goed, is \"{expr}\" wel een regular expression: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}", "HTML", true, false, message.MessageID);
            }
        }

        private string GetCommandFromMessage(Message message, out string argstr, out string[] args)
        {
            try
            {
                MessageEntity firstEntity = message.Entities?.FirstOrDefault();
                string command = null;
                args = null;
                argstr = null;
                if (firstEntity != null && firstEntity.Offset == 0 && firstEntity.Type == "bot_command")
                {
                    string me = Client.GetMe()?.Username;
                    command = message.Text.Substring(firstEntity.Offset, firstEntity.Length);
                    if (command.EndsWith($"@{me}"))
                    {
                        command = command.Substring(0, command.Length - me.Length + 1);
                    }
                    argstr = message.Text.Substring(firstEntity.Length)?.TrimStart();
                    args = new string[] { };
                    if (!string.IsNullOrEmpty(argstr))
                    {
                        args = argstr.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    }
                }
                return command?.ToLowerInvariant();
            }
            catch (Exception ex)
            {
                args = null;
                argstr = null;
                _log.Error(ex, $"Error, ignoring as command");
                return null;
            }
        }
    }
}
