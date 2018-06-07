using System.Collections.Generic;
using System.IO;

namespace PokemonRaidBot.Utils
{
    public static class Pokedex
    {
        public class PokedexEntry
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public static List<PokedexEntry> All { get; private set; }

        static Pokedex()
        {
            string pokedexJSON = File.ReadAllText("pokemon-list.json");
            All = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PokedexEntry>>(pokedexJSON);
        }
    }
}
