using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class Audio
    {
        [DeserializeAs(Name = "file_id")] public string FileID { get; set; }
        [DeserializeAs(Name = "duration")] public int DurationInSeconds { get; set; }
        [DeserializeAs(Name = "performer")] public string Performer { get; set; }
        [DeserializeAs(Name = "title")] public string Title { get; set; }
        [DeserializeAs(Name = "mime_type")] public string MimeType { get; set; }
        [DeserializeAs(Name = "file_size")] public long FileSize { get; set; }

    }
}