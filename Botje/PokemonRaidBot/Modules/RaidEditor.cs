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
using System.Text;

namespace PokemonRaidBot.Modules
{
    public class RaidEditor : IBotModule
    {
        private ILogger _log;

        private const string QrEditGym = "qe.egy"; // qe.egy:{raid}
        private const string QrEditPokemon = "qe.epo"; // qe.epo:{raid}
        private const string QrEditTime = "qe.eti"; // qe.eti:{raid}
        private const string QrEditAlignment = "qe.eda"; // qe.eda:{raid}
        private const string QrEditRemarks = "qe.edr"; // qe.edr:{raid}
        private const string QrEditLocation = "qe.elo"; // qe.elo:{raid}
        private const string QrUnpublish = "qe.unp"; // qe.unp:{raid}
        private const string QrPublish = "qe.pub"; // qe.pub:{raid}
        private const string QrRefresh = "qe.rf5"; // qe.rf5:{raid}
        private const string QrAlignmentSelected = "qe.als"; // qe.als:{raid}:{teamid}

        private const string StateExpectLocation = "qe.state.expect.location";
        private const string StateExpectGym = "qe.state.expect.gym";
        private const string StateExpectPokemon = "qe.state.expect.pokemon";
        private const string StateExpectTime = "qe.state.expect.time";
        private const string StateExpectRemarks = "qe.state.expect.remarks";
        private const string IqPrefix = "qr-";

        [Inject]
        public ITimeService TimeService { get; set; }

        [Inject]
        public ILocationToAddressService AddressServicie { get; set; }

        [Inject]
        public ICatalog I18N { get; set; }
        protected readonly Func<string, string> _HTML_ = (s) => MessageUtils.HtmlEscape(s);

        [Inject]
        public ILoggerFactory LoggerFactory { set { _log = value.Create(GetType()); } }

        [Inject]
        public IDatabase DB { get; set; }

        [Inject]
        public IMessagingClient Client { get; set; }

        [Inject]
        public ISettingsManager Settings { get; set; }

        [Inject]
        public IPrivateConversationManager ConversationManager { get; set; }

        public void Shutdown()
        {
            Client.OnQueryCallback -= Client_OnQueryCallback;
            Client.OnPrivateMessage -= Client_OnPrivateMessage;
        }

        public void Startup()
        {
            Client.OnQueryCallback += Client_OnQueryCallback;
            Client.OnPrivateMessage += Client_OnPrivateMessage;
        }

        private void Client_OnPrivateMessage(object sender, PrivateMessageEventArgs e)
        {
            string state = ConversationManager.GetState(e.Message.From, out string[] data);

            switch (state)
            {
                case StateExpectLocation:
                    if (e.Message.Location != null)
                    {
                        UpdateLocation(data[0], e.Message.From, e.Message.Location);
                    }
                    else
                    {
                        Client.SendMessageToChat(e.Message.From.ID, I18N.GetString("I expected a location."));
                    }
                    EditRaid(e.Message.From, data[0]);
                    break;

                case StateExpectGym:
                    DoForPublicRaid(data[0], (raidCollection, raid) =>
                    {
                        raid.Raid.Gym = e.Message.Text;
                        raid.LastModificationTime = DateTime.UtcNow;
                        raidCollection.Update(raid);
                        EditRaid(e.Message.From, data[0]);
                    });
                    break;

                case StateExpectPokemon:
                    DoForPublicRaid(data[0], (raidCollection, raid) =>
                    {
                        raid.Raid.Raid = e.Message.Text;
                        raid.LastModificationTime = DateTime.UtcNow;
                        raidCollection.Update(raid);
                        EditRaid(e.Message.From, data[0]);
                    });
                    break;

                case StateExpectRemarks:
                    DoForPublicRaid(data[0], (raidCollection, raid) =>
                    {
                        raid.Raid.Remarks = e.Message.Text;
                        raid.LastModificationTime = DateTime.UtcNow;
                        raidCollection.Update(raid);
                        EditRaid(e.Message.From, data[0]);
                    });
                    break;

                case StateExpectTime:
                    DoForPublicRaid(data[0], (raidCollection, raid) =>
                    {
                        bool okay = false;
                        string[] timeStr = e.Message.Text.Trim().Split(':');
                        if (timeStr.Count() == 2)
                        {
                            if (int.TryParse(timeStr[0], out int hours)
                                && int.TryParse(timeStr[1], out int minutes)
                                && (hours >= 0) && (hours <= 23)
                                && (minutes >= 0) && (minutes <= 59))
                            {
                                DateTime localRaidTime = raid.Raid.RaidUnlockTime + TimeUtils.TzInfo.GetUtcOffset(raid.Raid.RaidUnlockTime);
                                DateTime newLocalRaidTime = new DateTime(localRaidTime.Year, localRaidTime.Month, localRaidTime.Day, hours, minutes, 0);
                                DateTime newRaidTimeUTC = TimeUtils.ToUTC(newLocalRaidTime);
                                raid.Raid.RaidUnlockTime = newRaidTimeUTC;
                                raid.Raid.RaidEndTime = newRaidTimeUTC + TimeSpan.FromMinutes(RaidCreationWizard.RaidDurationInMinutes);
                                raid.LastModificationTime = DateTime.UtcNow;
                                raidCollection.Update(raid);
                                EditRaid(e.Message.From, data[0]);
                            }
                        }
                        if (!okay)
                        {
                            Client.SendMessageToChat(e.Message.From.ID, I18N.GetString("Can't process input. Next time just send me a single line of text in the form HH:MM where HH is a number between 00 and 23, and MM is a number of minutes."));
                        }

                        raid.Raid.Remarks = e.Message.Text;
                        raid.LastModificationTime = DateTime.UtcNow;
                        raidCollection.Update(raid);
                        EditRaid(e.Message.From, data[0]);
                    });
                    break;
            }
        }

        private void UpdateLocation(string raidPublicID, User user, Location location)
        {
            DoForPublicRaid(raidPublicID, (raidCollection, raid) =>
            {
                raid.Raid.Location = location;
                raid.LastModificationTime = DateTime.UtcNow;
                raidCollection.Update(raid);
                UpdateAddress(raidCollection, raid, location);
            });
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

        private void Client_OnQueryCallback(object sender, QueryCallbackEventArgs e)
        {
            string command = e.CallbackQuery.Data.Split(':').FirstOrDefault();
            string[] args = e.CallbackQuery.Data.Split(':').Skip(1).ToArray();
            switch (command)
            {
                case QrEditLocation:
                    ConversationManager.SetState(e.CallbackQuery.From, StateExpectLocation, args);
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID);
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, I18N.GetString("Please send me a location using the attach-button in your messaging client."));
                    break;
                case QrEditGym:
                    ConversationManager.SetState(e.CallbackQuery.From, StateExpectGym, args);
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID);
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, I18N.GetString("Please send me the updated name of the gym."));
                    break;
                case QrEditPokemon:
                    ConversationManager.SetState(e.CallbackQuery.From, StateExpectPokemon, args);
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID);
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, I18N.GetString("Please send me the updated name of the Pokémon."));
                    break;
                case QrEditRemarks:
                    ConversationManager.SetState(e.CallbackQuery.From, StateExpectRemarks, args);
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID);
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, I18N.GetString("Please send me a line of text to add as remarks to the raid."));
                    break;
                case QrEditTime:
                    ConversationManager.SetState(e.CallbackQuery.From, StateExpectTime, args);
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID);
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, I18N.GetString("Enter the exact time that the egg with hatch in the form HH:MM using a 24-hour clock."));
                    break;
                case QrEditAlignment:
                    ConversationManager.SetState(e.CallbackQuery.From, null);
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, I18N.GetString("Select the new gym alignment from the menu"));
                    Client.SendMessageToChat(e.CallbackQuery.From.ID, I18N.GetString("Select the new gym alignment from the menu."), "HTML", true, true, null, CreateAlignmentMenu(args[0]));
                    break;
                case QrAlignmentSelected: // :{raid}:{alignment}
                    var team = (Team)Int32.Parse(args[1]);
                    _log.Info($"{e.CallbackQuery.From.DisplayName()} changed the gym alignment for {args[0]} to {team}");
                    Client.EditMessageText($"{e.CallbackQuery.Message.Chat.ID}", e.CallbackQuery.Message.MessageID, null, _HTML_(I18N.GetString("Updated the gym alignment. Manual refresh of the published raid is required.")));
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Thanks. Now please manually refresh the published message.")));
                    UpdateGymAlignment(e.CallbackQuery.From, args[0], team);
                    break;
                case QrUnpublish:
                    DoForPublicRaid(args[0], (raidCollection, raid) =>
                    {
                        if (raid.IsPublished)
                        {
                            raid.IsPublished = false;
                            if (raid.Raid.TelegramMessageID.HasValue)
                            {
                                var channelID = Settings.PublicationChannel.Value;
                                Client.DeleteMessage(channelID, raid.Raid.TelegramMessageID.Value);
                                raid.Raid.TelegramMessageID = null;
                                raid.LastModificationTime = DateTime.UtcNow;
                                raidCollection.Update(raid);
                                Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Publication of this raid has been undone. You may publish the raid again at your discretion.")));
                            }
                        }
                        else
                        {
                            Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("This raid is not published (anymore). You may publish the raid again.")));
                        }
                        EditRaid(e.CallbackQuery.From, args[0]);
                    });
                    break;
                case QrRefresh:
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Refreshing...")));
                    EditRaid(e.CallbackQuery.From, args[0]);
                    break;
                default:
                    break;
            }
        }

        private void UpdateGymAlignment(User from, string raidPublicID, Team team)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (RaidParticipation.Lock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidPublicID).First();
                if (null != raid)
                {
                    raid.Raid.Alignment = team;
                    raid.LastModificationTime = DateTime.UtcNow;
                    raidCollection.Update(raid);
                    EditRaid(from, raidPublicID);
                }
            }
        }

        private InlineKeyboardMarkup CreateAlignmentMenu(string raidPublicID)
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
                    callback_data = $"{QrAlignmentSelected}:{raidPublicID}:{(int)value}"
                });
                result.inline_keyboard.Add(row);
            }

            return result;
        }

        internal void EditRaid(User user, string raidPublicID)
        {
            ConversationManager.SetState(user, null);

            DoForPublicRaid(raidPublicID, (raidCollection, raid) =>
            {
                var record = raid.Raid;

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
                text.AppendLine($"<b>" + _HTML_(I18N.GetString("Remarks")) + $"</b>: {record.Remarks}");

                string shareString = $"{IqPrefix}{raid.PublicID}";

                InlineKeyboardMarkup keyboardMarkup = new InlineKeyboardMarkup();
                keyboardMarkup.inline_keyboard = new List<List<InlineKeyboardButton>>();

                List<InlineKeyboardButton> row;
                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton { text = I18N.GetString("Gym"), callback_data = $"{QrEditGym}:{raidPublicID}" });
                row.Add(new InlineKeyboardButton { text = I18N.GetString("Pokémon"), callback_data = $"{QrEditPokemon}:{raidPublicID}" });
                keyboardMarkup.inline_keyboard.Add(row);
                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton { text = I18N.GetString("Alignment"), callback_data = $"{QrEditAlignment}:{raidPublicID}" });
                row.Add(new InlineKeyboardButton { text = I18N.GetString("Time"), callback_data = $"{QrEditTime}:{raidPublicID}" });
                keyboardMarkup.inline_keyboard.Add(row);
                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton { text = I18N.GetString("Remarks"), callback_data = $"{QrEditRemarks}:{raidPublicID}" });
                row.Add(new InlineKeyboardButton { text = I18N.GetString("Location"), callback_data = $"{QrEditLocation}:{raidPublicID}" });
                keyboardMarkup.inline_keyboard.Add(row);
                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("Share")), switch_inline_query = $"{shareString}" });
                keyboardMarkup.inline_keyboard.Add(row);
                row = new List<InlineKeyboardButton>();
                if (raid.IsPublished)
                {
                    row.Add(new InlineKeyboardButton { text = I18N.GetString("Unpublish"), callback_data = $"{QrUnpublish}:{raidPublicID}" });
                    keyboardMarkup.inline_keyboard.Add(row);
                    row = new List<InlineKeyboardButton>();
                }
                else
                {
                    row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("📣 Publish")), callback_data = $"{RaidEventHandler.QrPublish}:{raidPublicID}" });
                    keyboardMarkup.inline_keyboard.Add(row);
                    row = new List<InlineKeyboardButton>();
                }

                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("🔄")), callback_data = $"{QrRefresh}:{raid.PublicID}" });
                keyboardMarkup.inline_keyboard.Add(row);

                try
                {
                    Client.SendMessageToChat(user.ID, text.ToString(), "HTML", true, true, null, keyboardMarkup);
                }
                catch (Exception ex)
                {
                    _log.Warn($"Could not start edit chat with user {user.ToString()}: {ex.GetType().Name} - {ex.Message}");
                }
            });
        }

        private void DoForPublicRaid(string publicRaidID, Action<DbSet<RaidParticipation>, RaidParticipation> action)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (RaidParticipation.Lock)
            {
                var raid = raidCollection.Find(x => x.PublicID == publicRaidID).First();
                if (null != raid) // okay, let's edit this raid
                {
                    action(raidCollection, raid);
                }
                else
                {
                    _log.Error($"Got request for raid ID {publicRaidID} but it does not exist.");
                }
            }
        }
    }
}
