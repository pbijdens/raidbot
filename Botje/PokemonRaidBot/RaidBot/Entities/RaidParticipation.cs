using Botje.Core.Utils;
using Botje.DB;
using Botje.Messaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonRaidBot.RaidBot.Entities
{
    public class RaidParticipation : IAtom
    {
        public Guid UniqueID { get; set; }

        public string PublicID { get; set; }

        public RaidDescription Raid { get; set; }

        public Dictionary<Team, List<UserParticipation>> Participants { get; set; }

        public List<User> Rejected { get; set; }

        public bool IsPublished { get; set; }

        public RaidParticipation()
        {
            PublicID = ShortGuid.NewGuid().ToString();
            Participants = new Dictionary<Team, List<UserParticipation>>();
            foreach (Team team in Enum.GetValues(typeof(Team)).OfType<Team>())
            {
                Participants[team] = new List<UserParticipation>();
            }
            Rejected = new List<User>();
        }
    }
}
