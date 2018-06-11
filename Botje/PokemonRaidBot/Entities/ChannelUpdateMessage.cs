using Botje.DB;
using System;

namespace PokemonRaidBot.Entities
{
    public class ChannelUpdateMessage : IAtom
    {
        public Guid UniqueID { get; set; }

        public long ChannelID { get; set; }

        public long MessageID { get; set; }

        public string Hash { get; set; }

        public DateTime LastModificationDate { get; set; }
    }
}
