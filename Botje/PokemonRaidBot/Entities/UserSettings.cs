using Botje.DB;
using Botje.Messaging.Models;
using System;
using System.Linq;

namespace PokemonRaidBot.Entities
{
    public class UserSettings : IAtom
    {
        public Guid UniqueID { get; set; }

        public User User { get; set; }

        public Team Team { get; set; }

        public string Alias { get; set; }

        public int Level { get; set; }

        // lock for adding/removing user settings
        public static object UserSettingsLock = new object();

        /// <summary>
        /// Returns the user's settings. If none present, creates them.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userSettingsCollection"></param>
        /// <returns></returns>
        public static UserSettings GetOrCreateUserSettings(User user, DbSet<UserSettings> userSettingsCollection)
        {
            lock (UserSettings.UserSettingsLock)
            {
                var result = userSettingsCollection.Find(x => x.User.ID == user.ID).FirstOrDefault();
                if (result == null)
                {
                    result = new UserSettings { User = user };
                    userSettingsCollection.Insert(result);
                }
                return result;
            }
        }
    }
}
