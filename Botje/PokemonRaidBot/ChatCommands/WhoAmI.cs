using Botje.Messaging.Models;

namespace PokemonRaidBot.ChatCommands
{
    public class WhoAmI : ChatCommandModuleBase
    {
        public override void ProcessCommand(Source source, Message message, string command, string[] args)
        {
            switch (command)
            {
                case "/whoami":
                    CmdWhoAmI(message.Chat.ID, message.From);
                    break;
            }
        }

        private void CmdWhoAmI(long conversationID, User who)
        {
            Client.SendMessageToChat(conversationID, $"<b>User:</b> " + _(who.ToString()));
        }
    }
}
