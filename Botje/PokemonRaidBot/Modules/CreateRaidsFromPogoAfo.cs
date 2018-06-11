using Botje.Core;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.Models;
using GeoCoordinatePortable;
using Ninject;
using PokemonRaidBot.Entities;
using PokemonRaidBot.LocationAPI;
using PokemonRaidBot.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace PokemonRaidBot.Modules
{
    /// <summary>
    /// Module that removes published raids from the channel.
    /// </summary>
    public class CreateRaidsFromPogoAfo : IBotModule
    {
        public static TimeSpan Interval = TimeSpan.FromSeconds(19);
        public static readonly string SourceID = "pogoafo.nl";

        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private Thread _thread;
        private ILogger _log;

        /// <summary></summary>
        [Inject]
        public IMessagingClient Client { get; set; }

        /// <summary></summary>
        [Inject]
        public IDatabase DB { get; set; }

        /// <summary></summary>
        [Inject]
        public ILoggerFactory LoggerFactory { set { _log = value.Create(GetType()); } }

        [Inject]
        public ISettingsManager Settings { get; set; }

        [Inject]
        public RaidEventHandler RaidEventHandler { get; set; }

        [Inject]
        public ISettingsManager SettingsManager { get; set; }

        [Inject]
        public ILocationToAddressService AddressServicie { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public void Shutdown()
        {
            _cts.Cancel();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Startup()
        {
            _thread = new Thread(Run);
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void Run()
        {
            var channelID = Settings.PublicationChannel.Value;
            _log.Info($"Starting worker thread for {nameof(CreateRaidsFromPogoAfo)}");
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    if (DateTime.UtcNow.Hour >= 21 || DateTime.UtcNow.Hour < 4)
                    {
                        _log.Trace($"Skipping {SourceID} update cycle because the server rests.");
                        continue;
                    }

                    foreach (var map in SettingsManager.PogoAfoMappings) // all configured mappings from pogoafo URLs to posting channels
                    {
                        try
                        {
                            _log.Trace($"Checking {map.Url}");
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(map.Url);
                            HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                using (var responseStream = response.GetResponseStream())
                                {
                                    using (var sr = new StreamReader(responseStream))
                                    {
                                        string responseString = sr.ReadToEnd();
                                        if (!string.IsNullOrWhiteSpace(responseString))
                                        {
                                            _log.Trace($"Response: {responseString}");

                                            var x = Pokedex.All.FirstOrDefault();
                                            var pogoAfoResult = Newtonsoft.Json.JsonConvert.DeserializeObject<PogoAfoResult>(responseString);
                                            if (string.IsNullOrWhiteSpace(pogoAfoResult.error))
                                            {
                                                CreateRaidsFromScanResult(map, pogoAfoResult, map.Channel);
                                            }
                                            else
                                            {
                                                _log.Warn($"{map.Url} returned an error '{pogoAfoResult.error}'. Skipping this iteration of the update loop.");
                                            }
                                        }
                                        else
                                        {
                                            _log.Warn($"{map.Url} returned empty response, but status code was 200.");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                _log.Warn($"{map.Url} failed to load with error code {response.StatusCode}, skipping this update loop.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, $"Failed to download {map.Url}");
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    _log.Info($"Abort requested for thread.");
                    break;
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error in cleanup thread. Ignoring.");
                }
                finally
                {
                    Thread.Sleep(Interval);
                }
            }
            _log.Info($"Stopped worker thread for {nameof(CleanupChannel)}");
        }

        private void CreateRaidsFromScanResult(PogoAfoMapping map, PogoAfoResult pogoAfoResult, long channel)
        {
            var raidParticipationCollection = DB.GetCollection<RaidParticipation>();
            var raidsList = raidParticipationCollection.FindAll().ToArray();
            foreach (var entry in pogoAfoResult.raids ?? new Dictionary<string, PogoAfoRaidInfo>())
            {
                // TODO: Should be part of the query already, why ask for items then discard them?
                if (entry.Value.raid_level < 5 && !entry.Value.ex_trigger)
                {
                    _log.Trace($"{SourceID} - Ignoring raid {entry.Key} @{entry.Value.raid_battle} level: {entry.Value.raid_level} / trigger: {entry.Value.ex_trigger} / pokémon: {entry.Value.raid_pokemon_id} / gym: {entry.Value.name} / url: {entry.Value.url}");
                    continue;
                }

                if (map.NorthEastCorner != null && map.SouthWestCorner != null && entry.Value.latitude.HasValue && entry.Value.longitude.HasValue)
                {
                    if (!(Between(entry.Value.latitude.Value, map.NorthEastCorner.Latitude, map.SouthWestCorner.Latitude) && Between(entry.Value.longitude.Value, map.NorthEastCorner.Longitude, map.SouthWestCorner.Longitude)))
                    {
                        _log.Trace($"{SourceID} - Ignoring raid {entry.Key} @{entry.Value.raid_battle} based on location, it's outside my area of interest.");
                        continue;
                    }
                }

                _log.Info($"{SourceID} - Incoming raid {entry.Key} @{entry.Value.raid_battle} / pokémon: {entry.Value.raid_pokemon_id} / gym: {entry.Value.name} / url: {entry.Value.url}");

                DateTimeOffset raidStartTime = entry.Value.raid_battle.HasValue ? DateTimeOffset.FromUnixTimeSeconds(entry.Value.raid_battle ?? 0) : DateTimeOffset.MinValue;
                DateTimeOffset raidEndTime = entry.Value.raid_end.HasValue ? DateTimeOffset.FromUnixTimeSeconds(entry.Value.raid_end ?? 0) : DateTimeOffset.MinValue;

                // Look for this exact raid
                var raidStruct = raidsList.Where(x => (x.Raid.Sources != null) && (x.Raid.Sources.Where(s => s.SourceID == SourceID && s.ExternalID == entry.Key).Any())).FirstOrDefault();
                if (null != raidStruct)
                {
                    _log.Info($"Found existing raid with same external ID: pokémon: {entry.Value.raid_pokemon_id} / gym: {entry.Value.name} / url: {entry.Value.url} === id: {raidStruct.PublicID} / {raidStruct.Raid.Raid} / {raidStruct.Raid.Gym}");
                }

                bool updated = false;
                // search for a PUBLISHED raid that's similar in location and time
                if (null == raidStruct)
                {
                    raidStruct = raidsList.Where(rp =>
                    {
                        if (rp.Raid != null && rp.Raid.Location != null && raidEndTime != null && rp.Raid.RaidEndTime != null)
                        {
                            var loc1 = new GeoCoordinate(rp.Raid.Location.Latitude, rp.Raid.Location.Longitude);
                            var loc2 = new GeoCoordinate(entry.Value.latitude ?? 0, entry.Value.longitude ?? 0);
                            var distance = loc1.GetDistanceTo(loc2);
                            return (distance <= 50) && (Math.Abs((raidEndTime.UtcDateTime - rp.Raid.RaidEndTime).TotalSeconds) <= 300) && rp.IsPublished;
                        }
                        else
                        {
                            return false;
                        }
                    }).FirstOrDefault();

                    if (null != raidStruct)
                    {
                        _log.Info($"Merging raid pokémon: {entry.Value.raid_pokemon_id} / gym: {entry.Value.name} / url: {entry.Value.url} with existing raid id: {raidStruct.PublicID} / {raidStruct.Raid.Raid} / {raidStruct.Raid.Gym}");
                    }
                }

                // Create a new raid
                if (null == raidStruct)
                {
                    updated = true;

                    _log.Info($"{SourceID} - Adding new raid {entry.Key} @{entry.Value.raid_battle} / pokémon: {entry.Value.raid_pokemon_id} / gym: {entry.Value.name} / url: {entry.Value.url}");
                    raidStruct = new RaidParticipation
                    {
                        Raid = new RaidDescription
                        {
                            Alignment = Team.Unknown,
                            Sources = new List<ExternalSource>() {
                                new ExternalSource
                                {
                                    ExternalID = entry.Key,
                                    SourceID = SourceID,
                                    URL = entry.Value.url
                                }
                            },
                            User = new Botje.Messaging.Models.User { IsBot = true, ID = -1, FirstName = SourceID, LastName = SourceID, Username = SourceID },
                            UpdateCount = 1,
                        }
                    };
                    raidParticipationCollection.Insert(raidStruct);
                }

                // update raidStruct
                string currentValue = raidStruct.Raid.Raid;
                if (entry.Value.raid_pokemon_id != null)
                {
                    var pokedexEntry = Pokedex.All.Where(x => x.id == entry.Value.raid_pokemon_id).FirstOrDefault();
                    if (null == pokedexEntry)
                    {
                        raidStruct.Raid.Raid = $"Pokémon {entry.Value.raid_pokemon_id} (level {entry.Value.raid_level})";
                    }
                    else
                    {
                        raidStruct.Raid.Raid = $"{pokedexEntry.name} (level {entry.Value.raid_level})";
                    }
                }
                else
                {
                    raidStruct.Raid.Raid = $"Level {entry.Value.raid_level} raid";
                }
                if (currentValue != raidStruct.Raid.Raid) updated = true;

                if (raidStruct.Raid.RaidUnlockTime != raidStartTime.UtcDateTime) { raidStruct.Raid.RaidUnlockTime = raidStartTime.UtcDateTime; updated = true; }
                if (raidStruct.Raid.RaidEndTime != raidEndTime.UtcDateTime) { raidStruct.Raid.RaidEndTime = raidEndTime.UtcDateTime; updated = true; }
                if (raidStruct.Raid.Gym != entry.Value.name) { raidStruct.Raid.Gym = entry.Value.name; updated = true; }
                if (entry.Value.ex_trigger && string.IsNullOrEmpty(raidStruct.Raid.Remarks))
                {
                    raidStruct.Raid.Remarks = $"EX Raid Trigger";
                    updated = true;
                }

                if (raidStruct.Raid.Publications == null)
                {
                    raidStruct.Raid.Publications = new List<PublicationEntry>();
                }


                if (entry.Value.longitude.HasValue && entry.Value.latitude.HasValue && (null == raidStruct.Raid.Location || raidStruct.Raid.Location.Latitude != entry.Value.latitude.Value || raidStruct.Raid.Location.Longitude != entry.Value.longitude.Value))
                {
                    raidStruct.Raid.Location = new Location
                    {
                        Latitude = entry.Value.latitude.Value,
                        Longitude = entry.Value.longitude.Value
                    };
                    updated = true;
                }

                if (string.IsNullOrEmpty(raidStruct.Raid.Address))
                {
                    UpdateAddress(raidParticipationCollection, raidStruct, raidStruct.Raid.Location);
                }

                if (updated)
                {
                    if (!raidStruct.Raid.Publications.Where(x => x.ChannelID == channel).Any())
                    {
                        _log.Info($"{SourceID} - Publishing {entry.Key} @{entry.Value.raid_battle} / pokémon: {entry.Value.raid_pokemon_id} / gym: {entry.Value.name} / url: {entry.Value.url}");
                        var message = RaidEventHandler.ShareRaidToChat(raidStruct, channel);
                        if (channel == Settings.PublicationChannel)
                        {
                            raidStruct.Raid.TelegramMessageID = message?.MessageID;
                            raidStruct.IsPublished = true;
                        }
                        raidStruct.Raid.Publications.Add(new PublicationEntry
                        {
                            ChannelID = channel,
                            TelegramMessageID = message?.MessageID ?? long.MaxValue
                        });
                    }

                    raidStruct.LastModificationTime = DateTime.UtcNow;
                    raidParticipationCollection.Update(raidStruct);
                }
                else
                {
                    _log.Info($"Nothing changed for raid {raidStruct.PublicID}, not updating anything.");
                }
            }
        }

        private bool Between(double value, double v1, double v2)
        {
            return (value >= v1 && value <= v2) || (value >= v2 && value <= v1);
        }

        private void UpdateAddress(DbSet<RaidParticipation> raidCollection, RaidParticipation raid, Location location)
        {
            bool wasInTime = AddressServicie.GetAddress(location.Latitude, location.Longitude).ContinueWith((t) =>
            {
                raid.Raid.Address = t.Result;
                raid.LastModificationTime = DateTime.UtcNow;
                raidCollection.Update(raid);
            }).Wait(TimeSpan.FromSeconds(5));

            if (wasInTime)
            {
                _log.Trace("Got address in time...");
            }
            else
            {
                _log.Warn("Address arrived too late from Google, not waiting. We'll update when we get it.");
            }
        }

#pragma warning disable IDE1006 // Naming Styles, don't care for serialization classes.
        private class PogoAfoResult
        {
            public int? count { get; set; }
            public string error { get; set; }
            public string gemeente { get; set; }
            public int? max_lvl { get; set; }
            public int? min_lvl { get; set; }
            public Dictionary<string, PogoAfoRaidInfo> raids { get; set; }
        }

        public class PogoAfoRaidInfo
        {
            public string name { get; set; }
            public double? latitude { get; set; }
            public double? longitude { get; set; }
            public bool ex_trigger { get; set; }
            public long? raid_battle { get; set; }
            public long? raid_end { get; set; }
            public int? raid_level { get; set; }
            public int? raid_pokemon_cp { get; set; }
            public int? raid_pokemon_id { get; set; }
            public string url { get; set; }
        }
#pragma warning restore IDE1006 // Naming Styles
    }
}
