using Botje.DB;
using System;

namespace Botje.Messaging.Telegram
{
    public class TelegramBotState : IAtom
    {
        public Guid UniqueID { get; set; }

        public long LastProcessedUpdateID { get; set; }
    }
}