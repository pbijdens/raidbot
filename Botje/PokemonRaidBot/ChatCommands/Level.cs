using Botje.DB;
using Botje.Messaging.Models;
using PokemonRaidBot.Entities;

namespace PokemonRaidBot.ChatCommands
{
    public class Level : ChatCommandModuleBase
    {

        public override void ProcessCommand(Source source, Message message, string command, string[] args)
        {
            switch (command)
            {
                case "/level":
                    if (source == Source.Private)
                    {
                        DoLevelCommand(message, command, args);
                    }
                    break;
            }
        }

        private void DoLevelCommand(Message message, string command, string[] args)
        {
            var userSetting = GetOrCreateUserSettings(message.From, out DbSet<UserSettings> dbSetUserSettings);
            if (args.Length != 0)
            {
                lock (UserSettingsLock)
                {
                    int.TryParse(args[0], out int level);
                    if (level < 0 || level > 40)
                    {
                        Client.SendMessageToChat(message.Chat.ID, $"Haha, erg grappig.", "HTML", true, false, message.MessageID);
                        return;
                    }
                    userSetting.Level = level;
                    dbSetUserSettings.Update(userSetting);
                }
            }

            if (userSetting.Level > 0)
            {
                string msg = I18N.GetString("Your Pokémon Go trainer level now is {0}.\nUse '/level 0' to remove your level.", userSetting.Level);
                Client.SendMessageToChat(message.Chat.ID, msg, "HTML", true, false, message.MessageID);
            }
            else
            {
                string msg = I18N.GetString("Your Pokémon Go trainer level is currently not shown.\nUse '/level [yourlevel]' to set your level.");
                Client.SendMessageToChat(message.Chat.ID, msg, "HTML", true, false, message.MessageID);
            }
        }
    }
}
