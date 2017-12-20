namespace Botje.Messaging.Models
{
    public class InlineQueryResultArticle
    {
        //type String  Type of the result, must be article
        public string type { get { return "article"; } set { } }

        //id String  Unique identifier for this result, 1-64 Bytes
        public string id { get; set; }

        //title   String Title of the result
        public string title { get; set; }

        //description String  Optional.Short description of the result
        public string description { get; set; }

        //input_message_content   InputMessageContent Content of the message to be sent
        public InputMessageContent input_message_content { get; set; }

        //reply_markup InlineKeyboardMarkup    Optional.Inline keyboard attached to the message
        public InlineKeyboardMarkup reply_markup { get; set; }

        //url String  Optional.URL of the result
        //hide_url Boolean Optional.Pass True, if you don't want the URL to be shown in the message
        //thumb_url   String Optional.Url of the thumbnail for the result
        //thumb_width Integer Optional.Thumbnail width
        //thumb_height Integer Optional.Thumbnail height
    }
}
