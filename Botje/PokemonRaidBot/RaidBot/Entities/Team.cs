namespace PokemonRaidBot.RaidBot.Entities
{
    public enum Team : int
    {
        Unknown = 0,
        Valor = 1,
        Mystic = 2,
        Instinct = 3,
#if FEATURE_WITHHOLD_TEAM
        // Only used for testing the bot, not very useful in release mode.
        Withheld = 4,
#endif
    }
}
