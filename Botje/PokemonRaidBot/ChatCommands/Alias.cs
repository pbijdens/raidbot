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
                    userSetting.Alias = args[0];
                    dbSetUserSettings.Update(userSetting);
                }
            }

            if (!string.IsNullOrEmpty(userSetting.Alias))
            {
                Client.SendMessageToChat(message.Chat.ID, $"In de inschrijvingen sta je vermeld als \"{_(userSetting.Alias)}\"", "HTML", true, false, message.MessageID);
            }
            else
            {
                Client.SendMessageToChat(message.Chat.ID, $"In de inschrijvingen sta je vermeld met je reguliere username, namelijk \"{_(message.From.ShortName())}\".", "HTML", true, false, message.MessageID);
            }
        }
    }
}
