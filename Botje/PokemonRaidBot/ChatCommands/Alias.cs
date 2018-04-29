using Botje.DB;
using Botje.Messaging.Models;
using PokemonRaidBot.Entities;

namespace PokemonRaidBot.ChatCommands
{
    public class Alias : ChatCommandModuleBase
    {
        public override void ProcessCommand(Source source, Message message, string command, string[] args)
        {
            switch (command)
            {
                case "/alias":
                    if (source == Source.Private)
                    {
                        DoAliasCommand(message, command, args);
                    }
                    break;
            }
        }

        private void DoAliasCommand(Message message, string command, string[] args)
        {
            var userSetting = GetOrCreateUserSettings(message.From, out DbSet<UserSettings> dbSetUserSettings);
            if (args.Length != 0)
            {
                lock (UserSettingsLock)
                {
                    if (args[0] == "-")
                    {
                        userSetting.Alias = string.Empty;
                    }
                    else
                    {
                        userSetting.Alias = args[0];
                    }
                    dbSetUserSettings.Update(userSetting);
                }
            }

            if (!string.IsNullOrEmpty(userSetting.Alias))
            {
                string msg = I18N.GetString("Your alias in the subscriptions now is '{0}'. Remove the alias using the '/alias -' command.", _HTML_(userSetting.Alias));
                Client.SendMessageToChat(message.Chat.ID, msg, "HTML", true, false, message.MessageID);
            }
            else
            {
                string msg = I18N.GetString("Your regular username of '{0}' will be used in the subscription list.", _HTML_(message.From.ShortName()));
                Client.SendMessageToChat(message.Chat.ID, msg, "HTML", true, false, message.MessageID);
            }
        }
    }
}
