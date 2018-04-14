using Botje.DB;
using Botje.Messaging.Models;
using System;

namespace PokemonRaidBot.RaidBot.Entities
{
    public class RaidDescription : IAtom
    {
        /// <summary>
        /// required
        /// </summary>
        public Guid UniqueID { get; set; }

        // user for whom we create this raid, one allowed per user
        public User User { get; set; }

        // raid location
        public Location Location { get; set; }

        // address of the raid
        public String Address { get; set; }

        // goal of the raid
        public string Raid { get; set; }

        // name of the gym
        public string Gym { get; set; }

        // current alignmane of the gym
        public Team Alignment { get; set; }

        // UTC timestamp when the raid will unlock
        public DateTime RaidUnlockTime { get; set; }

        // UTC timestamp when the raid will end
        public DateTime RaidEndTime { get; set; }

        // number of updates to this structure
        public int UpdateCount { get; set; }

        // Remarks for this raid, could be ex-raid trigger or anything else
        public string Remarks { get; set; }
    }
}
