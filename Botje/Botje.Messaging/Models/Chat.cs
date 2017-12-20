using RestSharp.Deserializers;

namespace Botje.Messaging.Models
{
    public class Chat
    {
        //id Integer Unique identifier for this chat.This number may be greater than 32 bits and some programming languages may have difficulty/silent defects in interpreting it.But it is smaller than 52 bits, so a signed 64 bit integer or double-precision float type are safe for storing this identifier.
        [DeserializeAs(Name = "id")]
        public long ID { get; set; }

        //type String  Type of chat, can be either “private”, “group”, “supergroup” or “channel”
        [DeserializeAs(Name = "type")]
        public string Type { get; set; }

        //title String  Optional.Title, for supergroups, channels and group chats
        [DeserializeAs(Name = "title")]
        public string Title { get; set; }

        //username String  Optional.Username, for private chats, supergroups and channels if available
        [DeserializeAs(Name = "username")]
        public string Username { get; set; }

        //first_name  String Optional.First name of the other party in a private chat
        [DeserializeAs(Name = "first_name")]
        public string FirstName { get; set; }

        //last_name   String Optional.Last name of the other party in a private chat
        [DeserializeAs(Name = "last_name")]
        public string LastName { get; set; }

        public override string ToString()
        {
            return $"Chat(ID=\"{ID}\", Type=\"{Type}\", Title=\"{Title}\", Username=\"{Username}\", FirstName=\"{FirstName}\", LastName=\"{LastName}\")";
        }

        //all_members_are_administrators  Boolean Optional.True if a group has ‘All Members Are Admins’ enabled.
        //photo ChatPhoto   Optional.Chat photo. Returned only in getChat.
        //description String  Optional.Description, for supergroups and channel chats. Returned only in getChat.
        //invite_link String  Optional.Chat invite link, for supergroups and channel chats. Returned only in getChat.
        //pinned_message Message Optional.Pinned message, for supergroups.Returned only in getChat.
        //sticker_set_name String  Optional.For supergroups, name of group sticker set.Returned only in getChat.
        //can_set_sticker_set Boolean Optional.True, if the bot can change the group sticker set. Returned only in getChat.
    }
}
