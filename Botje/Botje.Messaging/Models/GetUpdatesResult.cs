using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class GetUpdatesResult
    {
        [DeserializeAs(Name = "update_id")]
        public long UpdateID { get; set; }

        [DeserializeAs(Name = "message")]
        public Message Message { get; set; }

        [DeserializeAs(Name = "edited_message")]
        public Message EditedMessage { get; set; }

        [DeserializeAs(Name = "channel_post")]
        public Message ChannelPost { get; set; }

        [DeserializeAs(Name = "edited_channel_post")]
        public Message EditedChannelPost { get; set; }

        [DeserializeAs(Name = "inline_query")]
        public InlineQuery InlineQuery { get; set; }

        [DeserializeAs(Name = "chosen_inline_result")]
        public ChosenInlineResult ChosenInlineResult { get; set; }

        [DeserializeAs(Name = "callback_query")]
        public CallbackQuery CallbackQuery { get; set; }

        public UpdateType GetUpdateType()
        {
            if (null != Message) return UpdateType.Message;
            if (null != EditedMessage) return UpdateType.EditedMessage;
            if (null != ChannelPost) return UpdateType.ChannelPost;
            if (null != EditedChannelPost) return UpdateType.EditedChannelPost;
            if (null != InlineQuery) return UpdateType.InlineQuery;
            if (null != ChosenInlineResult) return UpdateType.ChosenInlineResult;
            if (null != CallbackQuery) return UpdateType.CallbackQuery;
            return UpdateType.Unsuported;
        }
    }
}