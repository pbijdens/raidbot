using Botje.DB;
using Botje.Messaging.Models;
using System;

namespace PokemonRaidBot.RaidBot.Entities
{
    public class UserSettings : IAtom
    {
        public Guid UniqueID { get; set; }

        public User User { get; set; }

        public Team Team { get; set; }

        public string Alias { get; set; }
    }
}
