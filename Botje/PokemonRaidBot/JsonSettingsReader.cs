using System.Collections.Generic;
using System.IO;

namespace PokemonRaidBot
{
    /// <summary>
    /// Technically we would not need an inner-class to implement this interface, because just implementing the interface with getters and setters ad having a static method to deserialize to this type would do.
    /// </summary>
    public class JsonSettingsReader : ISettingsManager
    {
        private class Settings
        {
            public string BotKey { get; set; }
            public List<string> Timezones { get; set; }
            public List<string> AdministratorUsernames { get; set; }
            public long? PublicationChannel { get; set; }
            public string GoogleLocationAPIKey { get; set; }
            public string DataFolder { get; set; }
        }

        private Settings _settings;

        public string BotKey => _settings.BotKey;

        public string[] Timezones => _settings.Timezones?.ToArray();

        public string[] AdministratorUsernames => _settings.AdministratorUsernames?.ToArray();

        public long? PublicationChannel => _settings.PublicationChannel;

        public string GoogleLocationAPIKey => _settings.GoogleLocationAPIKey;

        public string DataFolder => _settings.DataFolder;

        public void Read(string filename, string fallback)
        {
            string json = File.ReadAllText(File.Exists(filename) ? filename : fallback);
            _settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(json);
        }
    }
}
