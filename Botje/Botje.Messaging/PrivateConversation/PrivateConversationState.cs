using Botje.DB;
using Botje.Messaging.Models;
using System;

namespace Botje.Messaging.PrivateConversation
{
    public class PrivateConversationState : IAtom
    {
        public Guid UniqueID { get; set; }

        public User User { get; set; }

        public string State { get; set; }

        public string[] Data { get; set; }
    }
}
