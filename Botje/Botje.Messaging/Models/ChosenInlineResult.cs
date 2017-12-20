using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class ChosenInlineResult
    {
        //result_id String  The unique identifier for the result that was chosen
        [DeserializeAs(Name = "result_id")]
        public string ResultID { get; set; }

        //from    User The user that chose the result
        [DeserializeAs(Name = "from")]
        public User From { get; set; }

        //location    Location Optional.Sender location, only for bots that require user location
        [DeserializeAs(Name = "location")]
        public Location Location { get; set; }

        //inline_message_id   String Optional. Identifier of the sent inline message. Available only if there is an inline keyboard attached to the message.Will be also received in callback queries and can be used to edit the message.
        [DeserializeAs(Name = "inline_message_id")]
        public string InlineMessageID { get; set; }

        //query String  The query that was used to obtain the result
        [DeserializeAs(Name = "query")]
        public string Query { get; set; }
    }
}