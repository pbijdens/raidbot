using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class Result<T>
    {
        [DeserializeAs(Name = "ok")]
        public bool OK { get; set; }

        [DeserializeAs(Name = "description")]
        public string Description { get; set; }

        [DeserializeAs(Name = "error_code")]
        public int ErrorCode { get; set; }

        [DeserializeAs(Name = "result")]
        public T Data { get; set; }
    }

    public class Result
    {
        [DeserializeAs(Name = "ok")]
        public bool OK { get; set; }

        [DeserializeAs(Name = "description")]
        public string Description { get; set; }

        [DeserializeAs(Name = "error_code")]
        public int ErrorCode { get; set; }
    }
}