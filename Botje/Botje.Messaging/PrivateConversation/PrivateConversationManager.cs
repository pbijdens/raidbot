using Botje.Core;
using Botje.DB;
using Botje.Messaging.Models;
using Ninject;
using System.Linq;

namespace Botje.Messaging.PrivateConversation
{
    /// <summary>
    /// Simple implementation for maintaining conversation state.
    /// </summary>
    public class PrivateConversationManager : IPrivateConversationManager
    {
        private ILogger _log;

        [Inject]
        public IDatabase DB { get; set; }

        [Inject]
        public ILoggerFactory LoggerFactory { set { _log = value.Create(GetType()); } }

        public string GetState(User user)
        {
            var collection = DB.GetCollection<PrivateConversationState>();
            var state = collection.Find(x => x.User.ID == user.ID).FirstOrDefault();
            if (null != state)
            {
                return state.State;
            }
            return null;
        }

        public void SetState(User user, string state)
        {
            _log.Trace($"Setting private conversation state to \"{state}\" for {user.DisplayName()}");

            var collection = DB.GetCollection<PrivateConversationState>();
            var stateObj = collection.Find(x => x.User.ID == user.ID).FirstOrDefault();
            if (null == stateObj)
            {
                stateObj = new PrivateConversationState { User = user, State = state };
                collection.Insert(stateObj);
            }
            else
            {
                stateObj.State = state;
                collection.Update(stateObj);
            }
        }
    }
}
