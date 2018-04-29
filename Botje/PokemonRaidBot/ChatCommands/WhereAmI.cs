using Botje.Messaging.Models;

namespace PokemonRaidBot.ChatCommands
{
    public class WhereAmI : ChatCommandModuleBase
    {
        public override void ProcessCommand(Source source, Message message, string command, string[] args)
        {
            switch (command)
            {
                case "/whereami":
                    CmdWhereAmI(message.Chat.ID);
                    break;
            }
        }

        private void CmdWhereAmI(long chatID)
        {
            Chat chat = Client.GetChat(chatID);
            Client.SendMessageToChat(chatID, $"<b>Chat:</b> " + _(chat.ToString()));
        }
    }
}
