using Botje.Core;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.Events;
using Ninject;
using System.Linq;

namespace PokemonRaidBot.RaidBot
{
    public class WhereAmI : IBotModule
    {
        private ILogger _log;

        [Inject]
        public IMessagingClient Client { get; set; }

        [Inject]
        public IDatabase DB { get; set; }

        [Inject]
        public ILoggerFactory LoggerFactory { set { _log = value.Create(GetType()); } }

        [Inject]
        public ISettingsManager Settings { get; set; }

        public void Startup()
        {
            _log.Trace($"Started {GetType().Name}");
            Client.OnPrivateMessage += Client_OnPrivateMessage;
            Client.OnChannelMessage += Client_OnChannelMessage;
            Client.OnPublicMessage += Client_OnPublicMessage;
        }

        public void Shutdown()
        {
            Client.OnPrivateMessage -= Client_OnPrivateMessage;
            Client.OnChannelMessage -= Client_OnChannelMessage;
            Client.OnPublicMessage -= Client_OnPublicMessage;
            _log.Trace($"Shut down {GetType().Name}");
        }

        private void Client_OnPublicMessage(object sender, PublicMessageEventArgs e)
        {
            var me = Client.GetMe();
            var firstEntity = e.Message?.Entities?.FirstOrDefault();
            if (null != firstEntity && firstEntity.Type == "bot_command" && firstEntity.Offset == 0)
            {
                string commandText = e.Message.Text.Substring(firstEntity.Offset, firstEntity.Length);
                if (commandText == "/whereami" || commandText == $"/whereami@{me.Username}")
                {
                    Client.SendMessageToChat(e.Message.Chat.ID, $"<b>Public chat:</b> {e.Message.Chat.ID}");
                }
            }
        }

        private void Client_OnChannelMessage(object sender, ChannelMessageEventArgs e)
        {
            var me = Client.GetMe();
            var firstEntity = e.Message?.Entities?.FirstOrDefault();
            if (null != firstEntity && firstEntity.Type == "bot_command" && firstEntity.Offset == 0)
            {
                string commandText = e.Message.Text.Substring(firstEntity.Offset, firstEntity.Length);
                if (commandText == "/whereami" || commandText == $"/whereami@{me.Username}")
                {
                    Client.SendMessageToChat(e.Message.Chat.ID, $"<b>Channel:</b> {e.Message.Chat.ID}");
                }
            }
        }

        private void Client_OnPrivateMessage(object sender, PrivateMessageEventArgs e)
        {
            var me = Client.GetMe();
            var firstEntity = e.Message?.Entities?.FirstOrDefault();
            if (null != firstEntity && firstEntity.Type == "bot_command" && firstEntity.Offset == 0)
            {
                string commandText = e.Message.Text.Substring(firstEntity.Offset, firstEntity.Length);
                if (commandText == "/setchannel")
                {
                    if (Settings.AdministratorUsernames.Contains(e.Message.From.Username))
                    {
                        // TODO: Update the settings here to set the publication channel(s)
                        Client.SendMessageToChat(e.Message.Chat.ID, $"<b>Not implemented.</b>");
                    }
                }
                if (commandText == "/whereami")
                {
                    Client.SendMessageToChat(e.Message.Chat.ID, $"<b>Private chat:</b> {e.Message.Chat.ID}");
                }
            }
        }
    }
}
