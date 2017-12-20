using Botje.Messaging.Models;
using System;

namespace Botje.Messaging.Events
{
    public class PublicMessageEventArgs : EventArgs
    {
        public long UpdateID { get; private set; }
        public Message Message { get; private set; }

        public PublicMessageEventArgs(long updateID, Message editedMessage)
        {
            UpdateID = updateID;
            Message = editedMessage;
        }
    }
}