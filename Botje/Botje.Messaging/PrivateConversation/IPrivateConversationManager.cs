using Botje.Messaging.Models;

namespace Botje.Messaging.PrivateConversation
{
    /// <summary>
    /// For maintaining state for private chats with users, where state is limited to a single string.
    /// </summary>
    public interface IPrivateConversationManager
    {
        /// <summary>
        /// Retrieve user-state
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        string GetState(User user);

        /// <summary>
        /// Retrieve user-state and anys tored data for that state.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        string GetState(User user, out string[] data);

        /// <summary>
        /// Update user-state
        /// </summary>
        /// <param name="user"></param>
        /// <param name="state"></param>
        void SetState(User user, string state, string[] data = null);
    }
}
