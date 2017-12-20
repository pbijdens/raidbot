using Botje.Messaging.Models;
using System;

namespace Botje.Messaging.Events
{
    public class ChosenInlineQueryResultEventArgs : EventArgs
    {
        public long UpdateID { get; private set; }
        public ChosenInlineResult ChosenInlineResult { get; private set; }

        public ChosenInlineQueryResultEventArgs(long updateID, ChosenInlineResult chosenInlineResult)
        {
            UpdateID = updateID;
            ChosenInlineResult = chosenInlineResult;
        }
    }
}