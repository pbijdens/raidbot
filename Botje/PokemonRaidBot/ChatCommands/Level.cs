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
                Client.SendMessageToChat(message.Chat.ID, $"Je Pokémon GO level is nu \"{userSetting.Level}\".\r\n\r\nGebruik /level &lt;level&gt. om je level te veranderen (bijvoorbeeld /level 38).", "HTML", true, false, message.MessageID);
            }
            else
            {
                Client.SendMessageToChat(message.Chat.ID, $"Je Pokémon GO level wordt op dit moment niet getoond.\r\n\r\nGebruik /level &lt;level&gt. om je level in te stellen (bijvoorbeeld /level 38).", "HTML", true, false, message.MessageID);
            }
        }
    }
}
