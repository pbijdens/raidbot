using Botje.Messaging.Models;
using System;

namespace Botje.Messaging.Events
{
    public class QueryCallbackEventArgs : EventArgs
    {
        public long UpdateID { get; private set; }
        public CallbackQuery CallbackQuery { get; private set; }

        public QueryCallbackEventArgs(long updateID, CallbackQuery callbackQuery)
        {
            UpdateID = updateID;
            CallbackQuery = callbackQuery;
        }
    }
}