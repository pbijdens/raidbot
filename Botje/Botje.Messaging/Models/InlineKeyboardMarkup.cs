using System.Collections.Generic;

namespace Botje.Messaging.Models
{
    // This object represents an inline keyboard that appears right next to the message it belongs to.
    public class InlineKeyboardMarkup
    {
        public List<List<InlineKeyboardButton>> inline_keyboard { get; set; }
    }
}
