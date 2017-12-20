using RestSharp.Deserializers;
using System.Collections.Generic;

namespace Botje.Messaging.Models
{
    public class Message
    {
        // message_id Integer Unique message identifier inside this chat
        [DeserializeAs(Name = "message_id")]
        public long MessageID { get; set; }

        // from    User Optional.Sender, empty for messages sent to channels
        [DeserializeAs(Name = "from")]
        public User From { get; set; }

        // date Integer Date the message was sent in Unix time
        [DeserializeAs(Name = "date")]
        public long Date { get; set; }

        // chat Chat    Conversation the message belongs to
        [DeserializeAs(Name = "chat")]
        public Chat Chat { get; set; }

        // forward_from    User Optional. For forwarded messages, sender of the original message
        [DeserializeAs(Name = "forward_from")]
        public User ForwardFrom { get; set; }

        // forward_from_chat   Chat Optional. For messages forwarded from channels, information about the original channel
        [DeserializeAs(Name = "forward_from_chat")]
        public Chat ForwardFromChat { get; set; }

        // forward_from_message_id Integer Optional. For messages forwarded from channels, identifier of the original message in the channel
        [DeserializeAs(Name = "forward_from_message_id")]
        public long ForwardFromMessageId { get; set; }

        // forward_signature String  Optional.For messages forwarded from channels, signature of the post author if present
        [DeserializeAs(Name = "forward_signature")]
        public string ForwardSignature { get; set; }

        // forward_date    Integer Optional. For forwarded messages, date the original message was sent in Unix time
        [DeserializeAs(Name = "forward_date")]
        public long ForwardDate { get; set; }

        // reply_to_message Message Optional.For replies, the original message.Note that the Message object in this field will not contain further reply_to_message fields even if it itself is a reply.
        [DeserializeAs(Name = "reply_to_message")]
        public Message ReplyToMessage { get; set; }

        // edit_date Integer Optional.Date the message was last edited in Unix time
        [DeserializeAs(Name = "edit_date")]
        public long EditDate { get; set; }

        // author_signature    String Optional. Signature of the post author for messages in channels
        [DeserializeAs(Name = "author_signature")]
        public string AuthorSignature { get; set; }

        // text    String Optional. For text messages, the actual UTF-8 text of the message, 0-4096 characters.
        [DeserializeAs(Name = "text")]
        public string Text { get; set; }

        // entities Array of MessageEntity  Optional.For text messages, special entities like usernames, URLs, bot commands, etc.that appear in the text
        [DeserializeAs(Name = "entities")]
        public List<MessageEntity> Entities { get; set; }

        // caption_entities Array of MessageEntity  Optional.For messages with a caption, special entities like usernames, URLs, bot commands, etc.that appear in the caption
        [DeserializeAs(Name = "caption_entities")]
        public List<MessageEntity> CaptionEntities { get; set; }

        // caption String Optional. Caption for the audio, document, photo, video or voice, 0-200 characters
        [DeserializeAs(Name = "caption")]
        public string Caption { get; set; }

        // location Location    Optional.Message is a shared location, information about the location
        [DeserializeAs(Name = "location")]
        public Location Location { get; set; }

        // not supported for now because we don't need this

        // contact Contact Optional. Message is a shared contact, information about the contact
        // media_group_id String  Optional.The unique identifier of a media message group this message belongs to
        // audio Audio   Optional.Message is an audio file, information about the file
        // document Document    Optional.Message is a general file, information about the file
        // game Game    Optional.Message is a game, information about the game. More about games »
        // photo Array of PhotoSize  Optional.Message is a photo, available sizes of the photo
        // sticker Sticker Optional. Message is a sticker, information about the sticker
        // video Video   Optional.Message is a video, information about the video
        // voice Voice   Optional.Message is a voice message, information about the file
        // video_note VideoNote   Optional.Message is a video note, information about the video message
        // venue Venue   Optional.Message is a venue, information about the venue
        // new_chat_members Array of User   Optional.New members that were added to the group or supergroup and information about them (the bot itself may be one of these members)
        // left_chat_member User    Optional.A member was removed from the group, information about them(this member may be the bot itself)
        // new_chat_title String  Optional.A chat title was changed to this value
        // new_chat_photo  Array of PhotoSize Optional.A chat photo was change to this value
        // delete_chat_photo   True Optional. Service message: the chat photo was deleted
        // group_chat_created  True Optional. Service message: the group has been created
        // supergroup_chat_created True Optional. Service message: the supergroup has been created.This field can‘t be received in a message coming through updates, because bot can’t be a member of a supergroup when it is created.It can only be found in reply_to_message if someone replies to a very first message in a directly created supergroup.
        // channel_chat_created True    Optional.Service message: the channel has been created.This field can‘t be received in a message coming through updates, because bot can’t be a member of a channel when it is created.It can only be found in reply_to_message if someone replies to a very first message in a channel.
        // migrate_to_chat_id Integer Optional.The group has been migrated to a supergroup with the specified identifier. This number may be greater than 32 bits and some programming languages may have difficulty/silent defects in interpreting it. But it is smaller than 52 bits, so a signed 64 bit integer or double-precision float type are safe for storing this identifier.
        // migrate_from_chat_id Integer Optional.The supergroup has been migrated from a group with the specified identifier. This number may be greater than 32 bits and some programming languages may have difficulty/silent defects in interpreting it. But it is smaller than 52 bits, so a signed 64 bit integer or double-precision float type are safe for storing this identifier.
        // pinned_message Message Optional.Specified message was pinned. Note that the Message object in this field will not contain further reply_to_message fields even if it is itself a reply.
        // invoice Invoice Optional.Message is an invoice for a payment, information about the invoice. More about payments »
        // successful_payment SuccessfulPayment   Optional.Message is a service message about a successful payment, information about the payment. More about payments
    }
}