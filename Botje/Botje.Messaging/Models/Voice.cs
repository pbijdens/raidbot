using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class Voice
    {
        [DeserializeAs(Name = "file_id")] public string FileID { get; set; }
        [DeserializeAs(Name = "duration")] public int Duration { get; set; }
        [DeserializeAs(Name = "mime_type")] public string MimeType { get; set; }
        [DeserializeAs(Name = "file_size")] public long FileSize { get; set; }
    }
}