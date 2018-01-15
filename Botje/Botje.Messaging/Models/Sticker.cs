using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class Sticker
    {
        [DeserializeAs(Name = "file_id")] public string FileID { get; set; }
        [DeserializeAs(Name = "width")] public int Width { get; set; }
        [DeserializeAs(Name = "height")] public int Height { get; set; }
        [DeserializeAs(Name = "thumb")] public PhotoSize Thumb { get; set; }
        [DeserializeAs(Name = "emoji")] public string Emoji { get; set; }
        [DeserializeAs(Name = "set_name")] public string SetName { get; set; }
        [DeserializeAs(Name = "mask_position")] public MaskPosition MaskPosition { get; set; }
        [DeserializeAs(Name = "file_size")] public long FileSize { get; set; }

    }
}