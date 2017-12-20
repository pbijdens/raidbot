namespace Botje.Messaging.Models
{
    public class InputMessageContent
    {
        public string message_text { get; set; }
        public string parse_mode { get; set; }
        public bool? disable_web_page_preview { get; set; }
    }
}
