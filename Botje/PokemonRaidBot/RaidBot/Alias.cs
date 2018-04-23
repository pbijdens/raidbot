using Botje.Core;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.Events;
using Botje.Messaging.Models;
using Ninject;
using PokemonRaidBot.RaidBot.Entities;
using System;
using System.Linq;

namespace PokemonRaidBot.RaidBot
{
    public class Alias : IBotModule
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
        }

        public void Shutdown()
        {
            Client.OnPrivateMessage -= Client_OnPrivateMessage;
            _log.Trace($"Shut down {GetType().Name}");
        }

        private void Client_OnPrivateMessage(object sender, PrivateMessageEventArgs e)
        {
            var me = Client.GetMe();

            string command = GetCommandFromMessage(e.Message, out string argstr, out string[] args);
            ProcessCommand(e.Message, command, argstr, args);
        }

        private void ProcessCommand(Message message, string command, string argstr, string[] args)
        {
            switch (command ?? "")
            {
                case "/alias":
                    ShowOrCreateAlias(message, argstr, args);
                    break;
                default:
                    return;
            }
        }

        private void ShowOrCreateAlias(Message message, string argstr, string[] args)
        {
            var userSetting = GetOrCreateUserSettings(message.From, out DbSet<UserSettings> dbSetUserSettings);
            if (args.Length != 0)
            {
                lock (_userSettingsLock)
                {
                    userSetting.Alias = argstr;
                    dbSetUserSettings.Update(userSetting);
                }
            }

            if (!string.IsNullOrWhiteSpace(userSetting.Alias))
            {
                Client.SendMessageToChat(message.Chat.ID, $"In de inschrijvingen sta je vermeld als \"{userSetting.Alias}\"", "HTML", true, false, message.MessageID);
            }
            else
            {
                Client.SendMessageToChat(message.Chat.ID, $"In de inschrijvingen sta je vermeld met je reguliere username, namelijk \"{message.From.ShortName()}\".", "HTML", true, false, message.MessageID);
            }
        }

        private object _userSettingsLock = new object();
        private UserSettings GetOrCreateUserSettings(User user, out DbSet<UserSettings> userSettingsCollection)
        {
            lock (_userSettingsLock)
            {
                userSettingsCollection = DB.GetCollection<UserSettings>();
                var result = userSettingsCollection.Find(x => x.User.ID == user.ID).FirstOrDefault();
                if (result == null)
                {
                    result = new UserSettings { User = user };
                    userSettingsCollection.Insert(result);
                }
                return result;
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
