using Botje.Messaging.Models;
using System;

namespace Botje.Messaging.Events
{
    public class PrivateMessageEventArgs : EventArgs
    {
        public long UpdateID { get; private set; }
        public Message Message { get; private set; }

        public PrivateMessageEventArgs(long updateID, Message editedMessage)
        {
            UpdateID = updateID;
            Message = editedMessage;
        }
    }
}