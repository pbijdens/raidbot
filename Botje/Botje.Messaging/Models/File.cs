using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class File
    {
        [DeserializeAs(Name = "file_id")] public string FileID { get; set; }
        [DeserializeAs(Name = "file_size")] public long FileSize { get; set; }
        // https://api.telegram.org/file/bot<token>/<file_path>
        [DeserializeAs(Name = "file_path")] public string FilePath { get; set; }
    }
}