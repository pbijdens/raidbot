using Botje.Messaging.Models;
using System;

namespace Botje.Messaging.Events
{
    public class ChannelMessageEventArgs : EventArgs
    {
        public long UpdateID { get; private set; }
        public Message Message { get; private set; }

        public ChannelMessageEventArgs(long updateID, Message channelPost)
        {
            UpdateID = updateID;
            Message = channelPost;
        }
    }
}