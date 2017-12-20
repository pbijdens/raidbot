using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class Location
    {
        // longitude Float   Longitude as defined by sender
        [DeserializeAs(Name = "longitude")]
        public float Longitude { get; set; }

        // latitude    Float Latitude as defined by sender
        [DeserializeAs(Name = "latitude")]
        public float Latitude { get; set; }
    }
}