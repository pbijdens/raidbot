namespace Botje.Messaging.Models
{
    public class InlineKeyboardButton
    {
        //text	String	Label text on the button
        public string text { get; set; }

        //url	String	Optional. HTTP url to be opened when button is pressed
        /////// public string url { get; set; }

        //callback_data	String	Optional. Data to be sent in a callback query to the bot when button is pressed, 1-64 bytes
        public string callback_data { get; set; }

        //switch_inline_query	String	Optional. If set, pressing the button will prompt the user to select one of their chats, open that chat and insert the bot‘s username and the specified inline query in the input field. Can be empty, in which case just the bot’s username will be inserted.
        public string switch_inline_query { get; set; }

        //Note: This offers an easy way for users to start using your bot in inline mode when they are currently in a private chat with it. Especially useful when combined with switch_pm… actions – in this case the user will be automatically returned to the chat they switched from, skipping the chat selection screen.
        //switch_inline_query_current_chat	String	Optional. If set, pressing the button will insert the bot‘s username and the specified inline query in the current chat's input field. Can be empty, in which case only the bot’s username will be inserted.
        public string switch_inline_query_current_chat { get; set; }

        //This offers a quick way for the user to open your bot in inline mode in the same chat – good for selecting something from multiple options.
        //callback_game	CallbackGame	Optional. Description of the game that will be launched when the user presses the button.

        //NOTE: This type of button must always be the first button in the first row.
        //pay	Boolean	Optional. Specify True, to send a Pay button.

        //NOTE: This type of button must always be the first button in the first row.
    }
}