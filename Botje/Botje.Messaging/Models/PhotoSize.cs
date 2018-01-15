using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class PhotoSize
    {
        [DeserializeAs(Name = "file_id")] public string FileID { get; set; }
        [DeserializeAs(Name = "width")] public int Width { get; set; }
        [DeserializeAs(Name = "height")] public int Height { get; set; }
        [DeserializeAs(Name = "file_size")] public long FileSize { get; set; }

    }
}