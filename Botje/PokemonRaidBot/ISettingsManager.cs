using System.Collections.Generic;

namespace PokemonRaidBot
{
    public class PogoAfoMapping
    {
        public string Url { get; set; }
        public long Channel { get; set; }
    }

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
        List<PogoAfoMapping> PogoAfoMappings { get; }
    }
}
