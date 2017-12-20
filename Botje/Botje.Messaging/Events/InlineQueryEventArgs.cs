using Botje.Messaging.Models;
using System;

namespace Botje.Messaging.Events
{
    public class InlineQueryEventArgs : EventArgs
    {
        public long UpdateID { get; private set; }
        public InlineQuery Query { get; private set; }

        public InlineQueryEventArgs(long updateID, InlineQuery inlineQuery)
        {
            UpdateID = updateID;
            Query = inlineQuery;
        }
    }
}