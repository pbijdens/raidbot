using Botje.Core;
using Botje.Core.Utils;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.Events;
using Botje.Messaging.Models;
using Botje.Messaging.PrivateConversation;
using NGettext;
using Ninject;
using PokemonRaidBot.Entities;
using PokemonRaidBot.LocationAPI;
using PokemonRaidBot.RaidBot.Utils;
using PokemonRaidBot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace PokemonRaidBot.Modules
{
    public class RaidCreationWizard : IBotModule
    {
        private const string CbqRaid = "rcw.rai";
        private const string CbqGym = "rcw.gym";
        private const string CbqAlignment = "rcw.ali";
        private const string CbqTime = "rcw.tim";
        private const string CbqClear = "rcw.cle";
        private const string CbqDone = "rcw.don";
        private const string CbqAlignmentSelected = "rcw.sal";
        private const string CbqTimeSelected = "rcw.sti";
        private const string CbqManuallyEnterStartTime = "rcw.mes";
        private const string CbqOtherDateSelected = "rcw.ots";
        private const string CbqOtherDateTimeSelected = "rcw.odt";
        private const string CbqPlayerTeamSelected = "rcw.pts";

        private const string StateReadGym = "RCW-READ-GYM";
        private const string StateReadPokemon = "RCW-READ-RAID";

        public const int RaidDurationInMinutes = 45;
        public const int EggDurationInMinutes = 60;

        private ILogger _log;
        private RaidEventHandler _eventHandler;

        [Inject]
        public ITimeService TimeService { get; set; }

        /// <summary></summary>
        [Inject]
        public ICatalog I18N { get; set; }
        protected readonly Func<string, string> _HTML_ = (s) => MessageUtils.HtmlEscape(s);

        [Inject]
        public IMessagingClient Client { get; set; }

        [Inject]
        public IDatabase DB { get; set; }

        [Inject]
        public IPrivateConversationManager ConversationManager { get; set; }

        [Inject]
        public ILocationToAddressService AddressServicie { get; set; }

        [Inject]
        public ILoggerFactory LoggerFactory { set { _log = value.Create(GetType()); } }

        [Inject]
        public IBotModule[] Modules { set { _eventHandler = value.OfType<RaidEventHandler>().FirstOrDefault(); } }

        public void Startup()
        {
            _log.Trace($"Started {GetType().Name}");
            Client.OnPrivateMessage += Client_OnPrivateMessage;
            Client.OnQueryCallback += Client_OnQueryCallback;
        }

        public void Shutdown()
        {
            Client.OnPrivateMessage -= Client_OnPrivateMessage;
            Client.OnQueryCallback -= Client_OnQueryCallback;
            _log.Trace($"Shut down {GetType().Name}");
        }

        private void Client_OnQueryCallback(object sender, QueryCallbackEventArgs e)
        {
            string query = e.CallbackQuery.Data.Split(':').FirstOrDefault();
            switch (query)
            {
                case CbqGym:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID);
                    ConversationManager.SetState(e.CallbackQuery.From, StateReadGym);
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, _HTML_(I18N.GetString("Enter the name of the gym.")));
                    break;
                case CbqRaid:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID);
                    ConversationManager.SetState(e.CallbackQuery.From, StateReadPokemon);
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, _HTML_(I18N.GetString("Enter the name of the Pokémon.")));
                    break;
                case CbqAlignment:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID);
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, _HTML_(I18N.GetString("Specify the current gym's alignment.")), "HTML", true, true, null, CreateAlignmentMenu());
                    break;
                case CbqAlignmentSelected:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Updated the gym alignment.")));
                    var newValue = int.Parse(e.CallbackQuery.Data.Split(':')[1]);
                    UpdateAlignment(e.CallbackQuery.From, (Team)newValue);
                    ShowMenu(e.CallbackQuery.From);
                    break;
                case CbqTime:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID);
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, _HTML_(I18N.GetString("Please specify when the raid will take place. If the egg is yet to hatch, use one of the 🥚 buttons to indicate the amount of time left before the egg hatches. If the egg already hatched use one of the ⛔️ to indicate how many minutes are left for the raid.")), "HTML", true, true, null, CreateTimeMenu());
                    break;
                case CbqTimeSelected:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Raid time updated.")));
                    var minutesUntilRaidEnd = int.Parse(e.CallbackQuery.Data.Split(':')[1]);
                    UpdateTime(e.CallbackQuery.From, minutesUntilRaidEnd);
                    ShowMenu(e.CallbackQuery.From);
                    break;
                case CbqManuallyEnterStartTime:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID);
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, _HTML_(I18N.GetString("Select a date for this raid from the list below.")), "HTML", true, true, null, CreateDateMenu());
                    break;
                case CbqOtherDateSelected:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID);
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, _HTML_(I18N.GetString("Select a start time.")), "HTML", true, true, null, CreateTimeMenu(int.Parse(e.CallbackQuery.Data.Split(':')[1])));
                    break;
                case CbqOtherDateTimeSelected:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Raid time updated.")));
                    UpdateTime(e.CallbackQuery.From, int.Parse(e.CallbackQuery.Data.Split(':')[1]), int.Parse(e.CallbackQuery.Data.Split(':')[2]));
                    ShowMenu(e.CallbackQuery.From);
                    break;
                case CbqClear:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Cleared.")));
                    ResetConversation(e.CallbackQuery.From);
                    ShowMenu(e.CallbackQuery.From);
                    break;
                case CbqDone:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("The raid was created but not published yet. You can publish or share the raid with the corresponding buttons in the block.")));
                    ProcessRaidDone(e);
                    break;
                case CbqPlayerTeamSelected:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("If you say so.")));
                    var userSettings = GetOrCreateUserSettings(e.CallbackQuery.From, out var userSettingsCollection);
                    var team = (Team)(int.Parse(e.CallbackQuery.Data.Split(':')[1]));
                    userSettings.Team = team;
                    userSettingsCollection.Update(userSettings);
                    string msg = I18N.GetString("Your new team is {0}.", _HTML_(team.AsReadableString()));
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, msg, "HTML", true, true);
                    break;
            }
        }

        private object _userSettingsLock = new object();
        private UserSettings GetOrCreateUserSettings(User user, out DbSet<UserSettings> userSettingsCollection)
        {
            lock (_userSettingsLock)
            {
                userSettingsCollection = DB.GetCollection<UserSettings>();
                var result = userSettingsCollection.Find(x => x.User.ID == user.ID).FirstOrDefault();
                if (result == null)
                {
                    result = new UserSettings { User = user };
                    userSettingsCollection.Insert(result);
                }
                return result;
            }
        }

        private void ProcessRaidDone(QueryCallbackEventArgs e)
        {
            GetOrCreateRaidDescriptionForUser(e.CallbackQuery.From, out DbSet<RaidDescription> collection, out RaidDescription record);
            if (string.IsNullOrWhiteSpace(record.Raid))
            {
                Client.SendMessageToChat(e.CallbackQuery.From.ID, _HTML_(I18N.GetString("Please specify which Pokémon the raid is for. If unknown, just enter the level of the raid.")));
                ShowMenu(e.CallbackQuery.From);
            }
            else if (string.IsNullOrWhiteSpace(record.Gym))
            {
                Client.SendMessageToChat(e.CallbackQuery.From.ID, _HTML_(I18N.GetString("Please specify the name of the gym.")));
                ShowMenu(e.CallbackQuery.From);
            }
            else if (null == record.Location)
            {
                Client.SendMessageToChat(e.CallbackQuery.From.ID, _HTML_(I18N.GetString("Please specify the gym's location. You can send a location from the telegram mobile client by pressing the paperclip button and selecting Location. Then drag the map so the position marker is at the location of the gym. The address information will automatically be added by the bot.")));
                ShowMenu(e.CallbackQuery.From);
            }
            else if (record.RaidEndTime == default(DateTime) || record.RaidEndTime <= DateTime.UtcNow)
            {
                Client.SendMessageToChat(e.CallbackQuery.From.ID, _HTML_(I18N.GetString("Please specify when the raid will take place.")));
                ShowMenu(e.CallbackQuery.From);
            }
            else
            {
                _eventHandler.CreateAndSharePrivately(e.CallbackQuery.From, record);
                ResetConversation(e.CallbackQuery.From);
            }
        }

        private void Client_OnPrivateMessage(object sender, PrivateMessageEventArgs e)
        {
            if (e.Message.Type == MessageType.Document && (e.Message.Document?.Filename ?? "").ToLower().EndsWith(".json"))
            {
                ProcessJsonDocument(e);
            }
            else if (e.Message.Location != null) // getting a location message, update the current raid
            {
                UpdateLocation(e.Message.From, e.Message.Location);
                ShowMenu(e.Message.From);
            }
            else
            {
                // the messaging client should probably support bot commands :-)
                // now we are happy with any help that the telegram servers give us anduse the detected entities
                var firstEntity = e.Message?.Entities?.FirstOrDefault();
                if (null != firstEntity && firstEntity.Type == "bot_command" && firstEntity.Offset == 0)
                {
                    string commandText = e.Message.Text.Substring(firstEntity.Offset, firstEntity.Length);
                    if (commandText == "/start")
                    {
                        ResetConversation(e.Message.From);
                        ShowMenu(e.Message.From);
                    }
                    if (commandText == "/team")
                    {
                        ResetConversation(e.Message.From);
                        Client.SendMessageToChat(e.Message.From.ID, _HTML_(I18N.GetString("Pick your team from the list.")), "HTML", true, true, null, CreateTeamMenu());
                    }
                    if (commandText == "/help")
                    {
                        Client.SendMessageToChat(e.Message.From.ID, _HTML_(I18N.GetString("Send a location from the telegram mobile client by pressing the paperclip button and selecting Location. Then drag the map so the position marker is at the location of the gym. The address information will automatically be added by the bot.\nAfter having sent the location, will out the remaining details.\nFinally, when you are ready, you can choose to either publish the raid to the public channel, or share your raid privately using the share-button.")), "HTML");
                    }
                }
                else
                {
                    string state = ConversationManager.GetState(e.Message.From);
                    switch (state)
                    {
                        case StateReadGym:
                            UpdateGym(e.Message.From, e.Message.Text);
                            ShowMenu(e.Message.From);
                            break;
                        case StateReadPokemon:
                            UpdateRaid(e.Message.From, e.Message.Text);
                            ShowMenu(e.Message.From);
                            break;
                    }
                }
            }
        }

        public class JSONRequest
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Gym { get; set; }
            public Team GymAlignment { get; set; }
            public string RaidBoss { get; set; }
            public DateTimeOffset RaidEndTime { get; set; }
        }

        private void ProcessJsonDocument(PrivateMessageEventArgs e)
        {
            ResetConversation(e.Message.From);
            var doc = e.Message.Document;
            Client.SendMessageToChat(e.Message.From.ID, $"JSON: {MessageUtils.HtmlEscape(doc.Filename)} / {doc.FileSize} byte(s) / mime type {doc.MimeType}!", "HTML", true, null, e.Message.MessageID);

            try
            {
                File file = Client.GetFile(doc.FileID);
                var client = new WebClient();
                string jsonText = client.DownloadString($"{Client.FileBaseURL}/{file.FilePath}");

                JSONRequest request = Newtonsoft.Json.JsonConvert.DeserializeObject<JSONRequest>(jsonText);

                var user = e.Message.From;
                GetOrCreateRaidDescriptionForUser(user, out DbSet<RaidDescription> collection, out RaidDescription record);
                if (!String.IsNullOrWhiteSpace(request.Gym)) record.Gym = request.Gym;
                if (!String.IsNullOrWhiteSpace(request.RaidBoss)) record.Raid = request.RaidBoss;
                record.Alignment = request.GymAlignment;
                record.RaidEndTime = request.RaidEndTime.UtcDateTime;
                record.RaidUnlockTime = record.RaidEndTime - TimeSpan.FromMinutes(RaidDurationInMinutes);
                if (request.Latitude != 0 && !double.IsNaN(request.Latitude))
                {
                    record.Location = new Location { Latitude = (float)request.Latitude, Longitude = (float)request.Longitude };
                    collection.Update(record);
                    UpdateAddress(user, record.Location);
                }
                else
                {
                    collection.Update(record);
                }

                ShowMenu(e.Message.From);
            }
            catch (Exception ex)
            {
                var msg = ExceptionUtils.AsString(ex);
                Client.SendMessageToChat(e.Message.From.ID, $"<pre>{MessageUtils.HtmlEscape(msg)}</pre>", "HTML");
            }
        }

        private void UpdateGym(User user, string text)
        {
            ConversationManager.SetState(user, null);
            GetOrCreateRaidDescriptionForUser(user, out DbSet<RaidDescription> collection, out RaidDescription record);
            record.Gym = text;
            collection.Update(record);
        }

        private void UpdateRaid(User user, string text)
        {
            ConversationManager.SetState(user, null);
            GetOrCreateRaidDescriptionForUser(user, out DbSet<RaidDescription> collection, out RaidDescription record);
            record.Raid = text;
            collection.Update(record);
        }

        private void UpdateAlignment(User user, Team alignment)
        {
            GetOrCreateRaidDescriptionForUser(user, out DbSet<RaidDescription> collection, out RaidDescription record);
            record.Alignment = alignment;
            collection.Update(record);
        }

        private void UpdateTime(User user, int minutesUntilRaidEnd)
        {
            GetOrCreateRaidDescriptionForUser(user, out DbSet<RaidDescription> collection, out RaidDescription record);
            int minutesUntilRaidStart = minutesUntilRaidEnd - RaidDurationInMinutes;

            record.RaidEndTime = default(DateTime);
            record.RaidUnlockTime = default(DateTime);
            if (minutesUntilRaidEnd >= 0)
            {
                record.RaidEndTime = DateTime.UtcNow + TimeSpan.FromMinutes(minutesUntilRaidEnd);
            }
            if (minutesUntilRaidStart >= 0)
            {
                record.RaidUnlockTime = DateTime.UtcNow + TimeSpan.FromMinutes(minutesUntilRaidStart);
            }
            collection.Update(record);
        }

        private void UpdateTime(User user, int daysFromToday, int hour)
        {
            GetOrCreateRaidDescriptionForUser(user, out DbSet<RaidDescription> collection, out RaidDescription record);
            DateTime whenDate = DateTime.UtcNow + TimeSpan.FromDays(daysFromToday);
            DateTime raidStartTime = TimeUtils.ToUTC(new DateTime(whenDate.Year, whenDate.Month, whenDate.Day, hour, 0, 0));
            record.RaidUnlockTime = raidStartTime;
            record.RaidEndTime = raidStartTime + TimeSpan.FromMinutes(RaidDurationInMinutes);
            collection.Update(record);
        }

        private void UpdateLocation(User user, Location location)
        {
            GetOrCreateRaidDescriptionForUser(user, out DbSet<RaidDescription> collection, out RaidDescription record);
            record.Location = location;
            collection.Update(record);
            UpdateAddress(user, location);
        }

        private void UpdateAddress(User user, Location location)
        {
            // Wait synchronously for 5 seconds for the address service to come back with an answer. If it wasn't in time, return. If it arrives later, fine, use it anyway it will show upon the next update.
            bool wasInTime = AddressServicie.GetAddress(location.Latitude, location.Longitude).ContinueWith((t) =>
            {
                _log.Trace($"Received address: {t.Result}");
                GetOrCreateRaidDescriptionForUser(user, out DbSet<RaidDescription> collection, out RaidDescription record);
                record.Address = t.Result;
                collection.Update(record);
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

        private void GetOrCreateRaidDescriptionForUser(User user, out DbSet<RaidDescription> collection, out RaidDescription record)
        {
            collection = DB.GetCollection<RaidDescription>();
            record = collection.Find(x => x.User.ID == user.ID).FirstOrDefault();
            if (null == record)
            {
                record = new RaidDescription
                {
                    User = user,
                };
                collection.Insert(record);
            }
        }

        private void ShowMenu(User user)
        {
            GetOrCreateRaidDescriptionForUser(user, out DbSet<RaidDescription> collection, out RaidDescription record);

            StringBuilder text = new StringBuilder();
            text.AppendLine($"<b>" + _HTML_(I18N.GetString("Raid")) + $"</b>: {record.Raid}");
            text.AppendLine($"<b>" + _HTML_(I18N.GetString("Gym")) + $"</b>: {record.Gym}");
            text.AppendLine($"<b>" + _HTML_(I18N.GetString("Alignment")) + $"</b>: {record.Alignment.AsReadableString()}");
            text.AppendLine($"<b>" + _HTML_(I18N.GetString("Address")) + $"</b>: {record.Address}");
            text.AppendLine($"<b>" + _HTML_(I18N.GetString("GPS Location")) + $"</b>: lat={record.Location?.Latitude} lon={record.Location?.Longitude}");
            if (record.RaidUnlockTime != default(DateTime))
                text.AppendLine($"<b>" + _HTML_(I18N.GetString("Available from")) + $"</b>: {TimeService.AsShortTime(record.RaidUnlockTime)}");
            if (record.RaidEndTime != default(DateTime))
                text.AppendLine($"<b>" + _HTML_(I18N.GetString("Available until")) + $"</b>: {TimeService.AsShortTime(record.RaidEndTime)}");

            if (null == record.Location)
            {
                text.AppendLine($"\n");
                text.AppendLine(_HTML_(I18N.GetString("Please specify the gym's location. You can send a location from the telegram mobile client by pressing the paperclip button and selecting Location. Then drag the map so the position marker is at the location of the gym. The address information will automatically be added by the bot.")));
            }

            Client.SendMessageToChat(user.ID, text.ToString(), "HTML", true, true, null, CreateWizardMarkup(user, record));
        }

        private InlineKeyboardMarkup CreateAlignmentMenu()
        {
            InlineKeyboardMarkup result = new InlineKeyboardMarkup();
            result.inline_keyboard = new List<List<InlineKeyboardButton>>();

            List<InlineKeyboardButton> row;

            foreach (var value in Enum.GetValues(typeof(Team)).OfType<Team>())
            {
                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton
                {
                    text = $"{value.AsReadableString()}",
                    callback_data = $"{CbqAlignmentSelected}:{(int)value}"
                });
                result.inline_keyboard.Add(row);
            }

            return result;
        }

        private InlineKeyboardMarkup CreateTeamMenu()
        {
            InlineKeyboardMarkup result = new InlineKeyboardMarkup();
            result.inline_keyboard = new List<List<InlineKeyboardButton>>();

            List<InlineKeyboardButton> row;

            foreach (var value in Enum.GetValues(typeof(Team)).OfType<Team>())
            {
                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton
                {
                    text = $"{value.AsReadableString()}",
                    callback_data = $"{CbqPlayerTeamSelected}:{(int)value}"
                });
                result.inline_keyboard.Add(row);
            }

            return result;
        }

        private InlineKeyboardMarkup CreateTimeMenu()
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            for (int i = 0; i <= EggDurationInMinutes; i += 5)
            {
                buttons.Add(new InlineKeyboardButton { text = $"🥚: {i}m", callback_data = $"{CbqTimeSelected}:{i + RaidDurationInMinutes}" });
            }
            for (int i = 0; i <= RaidDurationInMinutes; i += 5)
            {
                buttons.Add(new InlineKeyboardButton { text = $"⛔️: {i}m", callback_data = $"{CbqTimeSelected}:{i}" });
            }
            buttons.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("Other...")), callback_data = $"{CbqManuallyEnterStartTime}" });

            InlineKeyboardMarkup result = new InlineKeyboardMarkup();
            result.inline_keyboard = SplitButtonsIntoLines(buttons, maxElementsPerLine: 5, maxCharactersPerLine: 30);
            return result;
        }

        private InlineKeyboardMarkup CreateDateMenu()
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            for (int i = 0; i < 30; i++)
            {
                DateTime dt = DateTime.Now + TimeSpan.FromDays(i);
                buttons.Add(new InlineKeyboardButton { text = $"{dt.Day}-{dt.Month}", callback_data = $"{CbqOtherDateSelected}:{i}" });
            }

            InlineKeyboardMarkup result = new InlineKeyboardMarkup();
            result.inline_keyboard = SplitButtonsIntoLines(buttons, maxElementsPerLine: 5, maxCharactersPerLine: 30);
            return result;
        }

        private InlineKeyboardMarkup CreateTimeMenu(int daysFromNow)
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            for (int i = 0; i < 24; i++)
            {
                buttons.Add(new InlineKeyboardButton { text = $"{i}:00", callback_data = $"{CbqOtherDateTimeSelected}:{daysFromNow}:{i}" });
            }

            InlineKeyboardMarkup result = new InlineKeyboardMarkup();
            result.inline_keyboard = SplitButtonsIntoLines(buttons, maxElementsPerLine: 5, maxCharactersPerLine: 30);
            return result;
        }

        private List<List<InlineKeyboardButton>> SplitButtonsIntoLines(List<InlineKeyboardButton> buttons, int maxElementsPerLine, int maxCharactersPerLine)
        {
            var result = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> currentLine = new List<InlineKeyboardButton>();
            int lineLength = 0;
            foreach (var button in buttons)
            {
                if (currentLine.Count >= 0 && (lineLength + button.text.Length >= maxCharactersPerLine || currentLine.Count + 1 > maxElementsPerLine || button.text.Length > 10))
                {
                    result.Add(currentLine);
                    currentLine = new List<InlineKeyboardButton>();
                    lineLength = 0;
                }

                currentLine.Add(button);
                lineLength += button.text.Length;
            }
            if (currentLine.Count > 0)
            {
                result.Add(currentLine);
            }
            return result;
        }

        private InlineKeyboardMarkup CreateWizardMarkup(User user, RaidDescription record)
        {
            InlineKeyboardMarkup result = new InlineKeyboardMarkup();
            result.inline_keyboard = new List<List<InlineKeyboardButton>>();

            List<InlineKeyboardButton> row;

            row = new List<InlineKeyboardButton>();
            row.Add(new InlineKeyboardButton
            {
                text = (string.IsNullOrEmpty(record.Raid) ? "" : "✅ ") + _HTML_(I18N.GetString("Raid")),
                callback_data = CbqRaid
            });
            row.Add(new InlineKeyboardButton
            {
                text = (string.IsNullOrEmpty(record.Gym) ? "" : "✅ ") + _HTML_(I18N.GetString("Gym")),
                callback_data = CbqGym
            });
            row.Add(new InlineKeyboardButton
            {
                text = _HTML_(I18N.GetString("Alignment")),
                callback_data = CbqAlignment
            });
            result.inline_keyboard.Add(row);

            row = new List<InlineKeyboardButton>();
            row.Add(new InlineKeyboardButton
            {
                text = (record.RaidEndTime != default(DateTime) && record.RaidEndTime >= DateTime.UtcNow ? "✅ " : "") + _HTML_(I18N.GetString("Time")),
                callback_data = CbqTime
            });
            row.Add(new InlineKeyboardButton
            {
                text = _HTML_(I18N.GetString("🗑 Reset")),
                callback_data = CbqClear
            });
            result.inline_keyboard.Add(row);

            row = new List<InlineKeyboardButton>();
            row.Add(new InlineKeyboardButton
            {
                text = _HTML_(I18N.GetString("💾 Done")),
                callback_data = CbqDone
            });
            result.inline_keyboard.Add(row);

            return result;
        }

        private void ResetConversation(User user)
        {
            var collection = DB.GetCollection<RaidDescription>();
            collection.Delete(x => x.User.ID == user.ID);
            ConversationManager.SetState(user, null);
        }
    }
}
