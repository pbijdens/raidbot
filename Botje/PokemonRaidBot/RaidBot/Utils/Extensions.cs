using PokemonRaidBot.RaidBot.Entities;

namespace PokemonRaidBot.RaidBot.Utils
{
    public static class Extensions
    {
        public static string AsReadableString(this Team team)
        {
            switch (team)
            {
                case Team.Valor:
                    return "Valor ❤️";
                case Team.Mystic:
                    return "Mystic 💙";
                case Team.Instinct:
                    return "Instinct 💛";
                case Team.Unknown:
                default:
                    return "Onbekend 🖤";
            }
        }
    }
}
