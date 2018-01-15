using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class Document
    {
        [DeserializeAs(Name = "file_id")] public string FileID { get; set; }
        [DeserializeAs(Name = "thumb")] public PhotoSize Thumb { get; set; }
        [DeserializeAs(Name = "file_name")] public string Filename { get; set; }
        [DeserializeAs(Name = "title")] public string Title { get; set; }
        [DeserializeAs(Name = "mime_type")] public string MimeType { get; set; }
        [DeserializeAs(Name = "file_size")] public long FileSize { get; set; }
    }
}