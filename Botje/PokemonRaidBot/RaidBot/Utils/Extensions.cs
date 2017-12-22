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
#if FEATURE_WITHHOLD_TEAM
                case Team.Withheld:
                    return "Withheld 💜";
#endif
                case Team.Unknown:
                default:
                    return "Onbekend 🖤";
            }
        }

        public static string AsIcon(this Team team)
        {
            switch (team)
            {
                case Team.Valor:
                    return "❤️";
                case Team.Mystic:
                    return "💙";
                case Team.Instinct:
                    return "💛";
#if FEATURE_WITHHOLD_TEAM
                case Team.Withheld:
                    return "💜";
#endif
                case Team.Unknown:
                default:
                    return "🖤";
            }
        }
    }
}
