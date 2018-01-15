using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class VideoNote
    {
        [DeserializeAs(Name = "file_id")] public string FileID { get; set; }
        [DeserializeAs(Name = "length")] public int Length { get; set; }
        [DeserializeAs(Name = "duration")] public int Duration { get; set; }
        [DeserializeAs(Name = "thumb")] public PhotoSize Thumb { get; set; }
        [DeserializeAs(Name = "file_size")] public long FileSize { get; set; }

    }
}