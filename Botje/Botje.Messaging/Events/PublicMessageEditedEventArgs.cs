using Botje.Messaging.Models;
using System;

namespace Botje.Messaging.Events
{
    public class PublicMessageEditedEventArgs : EventArgs
    {
        public long UpdateID { get; private set; }
        public Message Message { get; private set; }

        public PublicMessageEditedEventArgs(long updateID, Message editedMessage)
        {
            UpdateID = updateID;
            Message = editedMessage;
        }
    }
}