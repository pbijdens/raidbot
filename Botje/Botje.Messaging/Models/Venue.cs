using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class Venue
    {
        [DeserializeAs(Name = "location")] public Location Location { get; set; }
        [DeserializeAs(Name = "title")] public string Title { get; set; }
        [DeserializeAs(Name = "address")] public string Address { get; set; }
        [DeserializeAs(Name = "foursquare_id")] public string FoursquareID { get; set; }
    }
}