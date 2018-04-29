namespace PokemonRaidBot
{
    /// <summary>
    /// Settings manager.
    /// </summary>
    public interface ISettingsManager
    {
        string BotKey { get; }
        string[] Timezones { get; }
        long? PublicationChannel { get; }
        string GoogleLocationAPIKey { get; }
        string Language { get; } // valid culture
    }
}
