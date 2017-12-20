using Botje.Messaging.Models;
using System;

namespace Botje.Messaging.Events
{
    public class ChannelMessageEditedEventArgs : EventArgs
    {
        public long UpdateID { get; private set; }
        public Message Message { get; private set; }

        public ChannelMessageEditedEventArgs(long updateID, Message editedChannelPost)
        {
            UpdateID = updateID;
            Message = editedChannelPost;
        }
    }
}