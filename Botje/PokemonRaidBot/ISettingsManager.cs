namespace PokemonRaidBot
{
    /// <summary>
    /// Settings manager.
    /// </summary>
    public interface ISettingsManager
    {
        string BotKey { get; }
        string[] Timezones { get; }
        string[] AdministratorUsernames { get; }
        long? PublicationChannel { get; }
        string GoogleLocationAPIKey { get; }
    }
}
