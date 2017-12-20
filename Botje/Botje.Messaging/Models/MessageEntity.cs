using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class MessageEntity
    {
        // type	String	Type of the entity. Can be mention (@username), hashtag, bot_command, url, email, bold (bold text), italic (italic text), code (monowidth string), pre (monowidth block), text_link (for clickable text URLs), text_mention (for users without usernames)
        [DeserializeAs(Name = "type")]
        public string Type { get; set; }

        //offset Integer Offset in UTF-16 code units to the start of the entity
        [DeserializeAs(Name = "offset")]
        public int Offset { get; set; }

        //length Integer Length of the entity in UTF-16 code units
        [DeserializeAs(Name = "length")]
        public int Length { get; set; }

        //url String  Optional.For “text_link” only, url that will be opened after user taps on the text
        [DeserializeAs(Name = "url")]
        public string Url { get; set; }

        //user    User Optional.For “text_mention” only, the mentioned user
        [DeserializeAs(Name = "user")]
        public User User { get; set; }
    }
}
