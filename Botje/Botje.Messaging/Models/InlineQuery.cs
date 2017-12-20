using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class InlineQuery
    {
        //id String  Unique identifier for this query
        [DeserializeAs(Name = "id")]
        public string ID { get; set; }

        //from    User Sender
        [DeserializeAs(Name = "from")]
        public User From { get; set; }

        //location Location    Optional.Sender location, only for bots that request user location
        [DeserializeAs(Name = "location")]
        public Location Location { get; set; }

        //query   String Text of the query(up to 512 characters)
        [DeserializeAs(Name = "query")]
        public string Query { get; set; }

        //offset String  Offset of the results to be returned, can be controlled by the bot
        [DeserializeAs(Name = "offset")]
        public string Offset { get; set; }
    }
}