using Botje.Messaging.Models;
using System;

namespace Botje.Messaging.Events
{
    public class PrivateMessageEditedEventArgs : EventArgs
    {
        public long UpdateID { get; private set; }
        public Message Message { get; private set; }

        public PrivateMessageEditedEventArgs(long updateID, Message editedMessage)
        {
            UpdateID = updateID;
            Message = editedMessage;
        }
    }
}