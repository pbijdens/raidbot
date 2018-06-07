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
using PokemonRaidBot.RaidBot.Utils;
using PokemonRaidBot.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PokemonRaidBot.Modules
{
    public class RaidEventHandler : IBotModule
    {
        private ILogger _log;

        public const string QrPublish = "qr.pub"; // qr.pub:{raid}
        private const string QrJoin = "qr.joi"; // qr.joi:{raid}:{extra}:{team}
        private const string QrDecline = "qr.dec"; // qr.dec:{raid}
        private const string QrRefresh = "qr.ref"; // qr.ref:{raid}
        private const string QrSetTime = "qr.sti"; // qr.sti:{raid}:{ticks}
        private const string QrArrived = "qr.arr"; // qr.aee:{raid}
        private const string QrEdit = "qr.edt"; // qr.aee:{raid}
        private const string QrDone = "qr.dne"; // qr.dne:{raid}
        private const string QrMaybe = "qr.myb"; // qr.myb:{raid}
        private const string IqPrefix = "qr-";

        [Inject]
        public ITimeService TimeService { get; set; }

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

        [Inject]
        public RaidEditor RaidEditor { get; set; }

        public void Shutdown()
        {
            Client.OnInlineQuery -= Client_OnInlineQuery;
            Client.OnQueryCallback -= Client_OnQueryCallback;
        }

        public void Startup()
        {
            Client.OnInlineQuery += Client_OnInlineQuery;
            Client.OnQueryCallback += Client_OnQueryCallback;
        }

        private void Client_OnInlineQuery(object sender, InlineQueryEventArgs e)
        {
            if (e.Query.Query.StartsWith(IqPrefix))
            {
                _log.Info($"Inline query from {e.Query.From.DisplayName()} ({e.Query.From.ID}) for query {e.Query.Query}");

                e.Query.Query = e.Query.Query.TrimEnd('@');
                string raidID = e.Query.Query.Substring(IqPrefix.Length);
                var raidCollection = DB.GetCollection<RaidParticipation>();
                var raid = raidCollection.Find(x => x.PublicID == raidID).FirstOrDefault();

                List<InlineQueryResultArticle> results = new List<InlineQueryResultArticle>();

                if (null != raid)
                {
                    string text = CreateRaidText(raid);
                    var markup = CreateMarkupFor(raid);
                    results.Add(new InlineQueryResultArticle
                    {
                        id = raid.PublicID,
                        title = $"{raid.Raid.Raid} {TimeService.AsShortTime(raid.Raid.RaidUnlockTime)}-{TimeService.AsShortTime(raid.Raid.RaidEndTime)}",
                        description = _HTML_(I18N.GetString("{0} raid at {1} {2}", raid.Raid.Raid, raid.Raid.Gym, raid.Raid.Address)),
                        input_message_content = new InputMessageContent
                        {
                            message_text = text,
                            parse_mode = "HTML",
                            disable_web_page_preview = true,
                        },
                        reply_markup = markup,
                    });
                }

                Client.AnswerInlineQuery(e.Query.ID, results);
            }
        }

        private void Client_OnQueryCallback(object sender, QueryCallbackEventArgs e)
        {
            string command = e.CallbackQuery.Data.Split(':').FirstOrDefault();
            string[] args = e.CallbackQuery.Data.Split(':').Skip(1).ToArray();
            switch (command)
            {
                case QrArrived: // :raid
                    _log.Trace($"{e.CallbackQuery.From.DisplayName()} has arrived for raid {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Excellent! You arrived.")));
                    UpdateUserRaidJoinOrUpdateAttendance(e.CallbackQuery.From, args[0]);
                    UpdateUserRaidArrived(e.CallbackQuery.From, args[0]);
                    RequestUpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrDecline: // :raid
                    _log.Trace($"{e.CallbackQuery.From.DisplayName()} has declined for raid {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("That's too bad 😞")));
                    UpdateUserRaidNegative(e.CallbackQuery.From, args[0]);
                    RequestUpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrJoin: // :raid:extra:team
                    _log.Trace($"{e.CallbackQuery.From.DisplayName()} will join raid {args[0]} [{e.CallbackQuery.Data}]");
                    if (args.Length >= 3 && int.TryParse(args[2], out int teamID) && teamID >= (int)Team.Unknown && teamID <= (int)Team.Instinct)
                    {
                        _log.Trace($"{e.CallbackQuery.From.DisplayName()} joined team {((Team)teamID).AsReadableString()}");
                        Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Joined for team {0}", _HTML_(((Team)teamID).AsReadableString()))));
                        UpdateUserSettingsForTeam(e.CallbackQuery.From, (Team)teamID);
                    }
                    else
                    {
                        Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("You're on the list now.")));
                    }
                    // Because we updated the user settings first, the user will automatically be added or moved to the
                    // correct team by the join function.
                    UpdateUserRaidJoinOrUpdateAttendance(e.CallbackQuery.From, args[0]);
                    if (args.Length >= 2 && int.TryParse(args[1], out int extra) && extra >= 0)
                    {
                        UpdateUserRaidExtra(e.CallbackQuery.From, args[0], extra);
                    }
                    RequestUpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrPublish: // :raid
                    _log.Info($"{e.CallbackQuery.From.DisplayName()} published raid {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Publishing the raid.")));
                    PublishRaid(e.CallbackQuery.From, args[0]);
                    RequestUpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrRefresh: // :raid
                    _log.Trace($"{e.CallbackQuery.From.DisplayName()} refreshed {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Refreshing...")));
                    RequestUpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrSetTime: // :raid:ticks
                    UpdateUserRaidJoinOrUpdateAttendance(e.CallbackQuery.From, args[0]);
                    if (long.TryParse(args[1], out long ticks))
                    {
                        var utcWhen = new DateTime(ticks);
                        _log.Trace($"{e.CallbackQuery.From.DisplayName()} updated their time for raid {args[0]} to {TimeService.AsShortTime(utcWhen)}");
                        Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("You will be there at {0}.", TimeService.AsShortTime(utcWhen))));
                        UpdateUserRaidTime(e.CallbackQuery.From, args[0], utcWhen);
                    }
                    else
                    {
                        Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Error updating time.")));
                    }
                    RequestUpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrEdit: // :{raid}
                    _log.Info($"{e.CallbackQuery.From.DisplayName()} wants to edit raid {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Go to our private chat to edit the raid.")));
                    RaidEditor.EditRaid(e.CallbackQuery.From, args[0]);
                    break;
                case QrDone: // :raid
                    _log.Trace($"{e.CallbackQuery.From.DisplayName()} has done raid {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Registered. Thanks.")));
                    UpdateUserRaidDone(e.CallbackQuery.From, args[0]);
                    RequestUpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrMaybe: // :raid
                    _log.Trace($"{e.CallbackQuery.From.DisplayName()} has answered 'maybe' for {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, _HTML_(I18N.GetString("Registered. Thanks.")));
                    UpdateUserRaidMaybe(e.CallbackQuery.From, args[0]);
                    RequestUpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
            }
        }

        private void UpdateUserRaidJoinOrUpdateAttendance(User user, string raidID)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (RaidParticipation.Lock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = UserSettings.GetOrCreateUserSettings(user, DB.GetCollection<UserSettings>());
                raid.Rejected.RemoveAll(x => x.ID == user.ID);
                raid.Done.RemoveAll(x => x.ID == user.ID);
                raid.Maybe.RemoveAll(x => x.ID == user.ID);
                UserParticipation participation = null;
                // If the user's team changed, make sure their data is saved but their participation record
                // is removed from the 'wrong' faction.
                raid.Participants.ToList().ForEach(kvp =>
                {
                    participation = kvp.Value.Where(x => x.User.ID == user.ID).FirstOrDefault() ?? participation;
                    kvp.Value.RemoveAll(x => x.User.ID == user.ID);
                });
                // If there was no participation record, create an unlined record now.
                if (null == participation)
                {
                    participation = new UserParticipation { User = user };
                }
                // If the participation record was not in the correct list yet, add it and re-sort the list.
                if (!raid.Participants[userSettings.Team].Contains(participation))
                {
                    raid.Participants[userSettings.Team].Add(participation);
                    raid.Participants[userSettings.Team].Sort((x, y) => string.Compare(x.User.DisplayName(), y.User.DisplayName()));
                }
                raidCollection.Update(raid);
            }
        }

        private void UpdateUserRaidNegative(User user, string raidID)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (RaidParticipation.Lock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = UserSettings.GetOrCreateUserSettings(user, DB.GetCollection<UserSettings>());
                raid.Rejected.RemoveAll(x => x.ID == user.ID);
                raid.Done.RemoveAll(x => x.ID == user.ID);
                raid.Maybe.RemoveAll(x => x.ID == user.ID);
                raid.Participants.ToList().ForEach(kvp =>
                {
                    kvp.Value.RemoveAll(x => x.User.ID == user.ID);
                });
                raid.Rejected.Add(user);
                raid.Rejected.Sort((x, y) => string.Compare(x.DisplayName(), y.DisplayName()));
                raidCollection.Update(raid);
            }
        }

        private void UpdateUserRaidDone(User user, string raidID)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (RaidParticipation.Lock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = UserSettings.GetOrCreateUserSettings(user, DB.GetCollection<UserSettings>());
                raid.Rejected.RemoveAll(x => x.ID == user.ID);
                raid.Done.RemoveAll(x => x.ID == user.ID);
                raid.Maybe.RemoveAll(x => x.ID == user.ID);
                raid.Participants.ToList().ForEach(kvp =>
                {
                    kvp.Value.RemoveAll(x => x.User.ID == user.ID);
                });
                raid.Done.Add(user);
                raid.Done.Sort((x, y) => string.Compare(x.DisplayName(), y.DisplayName()));
                raidCollection.Update(raid);
            }
        }

        private void UpdateUserRaidMaybe(User user, string raidID)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (RaidParticipation.Lock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = UserSettings.GetOrCreateUserSettings(user, DB.GetCollection<UserSettings>());
                raid.Rejected.RemoveAll(x => x.ID == user.ID);
                raid.Done.RemoveAll(x => x.ID == user.ID);
                raid.Maybe.RemoveAll(x => x.ID == user.ID);
                raid.Participants.ToList().ForEach(kvp =>
                {
                    kvp.Value.RemoveAll(x => x.User.ID == user.ID);
                });
                raid.Maybe.Add(user);
                raid.Maybe.Sort((x, y) => string.Compare(x.DisplayName(), y.DisplayName()));
                raidCollection.Update(raid);
            }
        }

        private void UpdateUserRaidArrived(User user, string raidID)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (RaidParticipation.Lock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = UserSettings.GetOrCreateUserSettings(user, DB.GetCollection<UserSettings>());
                var participation = raid.Participants[userSettings.Team].Where(x => x.User.ID == user.ID).FirstOrDefault();
                if (null != participation)
                {
                    participation.UtcArrived = DateTime.UtcNow;
                    participation.UtcWhen = default(DateTime);
                    raidCollection.Update(raid);
                }
            }
        }

        private void UpdateUserRaidExtra(User user, string raidID, int extra)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (RaidParticipation.Lock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = UserSettings.GetOrCreateUserSettings(user, DB.GetCollection<UserSettings>());
                var participation = raid.Participants[userSettings.Team].Where(x => x.User.ID == user.ID).FirstOrDefault();
                if (null != participation)
                {
                    participation.Extra = extra;
                    raidCollection.Update(raid);
                }
            }
        }

        private void UpdateUserRaidTime(User user, string raidID, DateTime utcWhen)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (RaidParticipation.Lock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = UserSettings.GetOrCreateUserSettings(user, DB.GetCollection<UserSettings>());
                var participation = raid.Participants[userSettings.Team].Where(x => x.User.ID == user.ID).FirstOrDefault();
                if (null != participation)
                {
                    participation.UtcWhen = utcWhen;
                    participation.UtcArrived = default(DateTime);
                    raidCollection.Update(raid);
                }
            }
        }

        private void UpdateUserSettingsForTeam(User user, Team team)
        {
            var userSettingsCollection = DB.GetCollection<UserSettings>();
            var userSettings = UserSettings.GetOrCreateUserSettings(user, userSettingsCollection);
            userSettings.Team = team;
            userSettingsCollection.Update(userSettings);
        }

        private void PublishRaid(User from, string raidID)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            var raid = raidCollection.Find(x => x.PublicID == raidID).FirstOrDefault();
            if (!raid.IsPublished && Settings.PublicationChannel.HasValue)
            {
                raid.IsPublished = true;
                raidCollection.Update(raid);

                var message = ShareRaidToChat(raid, Settings.PublicationChannel.Value);
                raid.Raid.TelegramMessageID = message.MessageID;
                raidCollection.Update(raid);
            }
        }

        public void RequestUpdateRaidMessage(long? chatID, long? messageID, string inlineMessageId, string raidID, string chatType)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            var raid = raidCollection.Find(x => x.PublicID == raidID).FirstOrDefault();
            if (null == raid)
            {
                _log.Error($"Got update request for raid {raidID}, but I can't find it");
                return;
            }

            lock (RaidParticipation.Lock)
            {
                // For purposes of updating the channel, update these items
                raid.LastModificationTime = DateTime.UtcNow;
                raidCollection.Update(raid);
            }

            if (chatType != "channel")
            {
                UpdateRaidMessage(chatID, messageID, inlineMessageId, raidID, chatType);
            }
        }

        public void UpdateRaidMessage(long? chatID, long? messageID, string inlineMessageId, string raidID, string chatType)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            var raid = raidCollection.Find(x => x.PublicID == raidID).FirstOrDefault();
            if (null == raid)
            {
                _log.Error($"Got update request for raid {raidID}, but I can't find it");
                return;
            }

            string newText = CreateRaidText(raid);
            var newMarkup = CreateMarkupFor(raid);

            if (!string.IsNullOrEmpty(inlineMessageId))
            {
                Client.EditMessageText(null, null, inlineMessageId, newText, "HTML", true, newMarkup, chatType);
            }
            else
            {
                Client.EditMessageText($"{chatID}", messageID, null, newText, "HTML", true, newMarkup, chatType);
            }
        }

        internal void CreateAndSharePrivately(User from, RaidDescription record)
        {
            var raid = new RaidParticipation { Raid = record };
            var collection = DB.GetCollection<RaidParticipation>();
            collection.Insert(raid);
            ShareRaidToChat(raid, from.ID);
        }

        internal Message ShareRaidToChat(RaidParticipation raid, long chatID)
        {
            string text = CreateRaidText(raid);
            InlineKeyboardMarkup markup = CreateMarkupFor(raid);
            var message = Client.SendMessageToChat(chatID, text, "HTML", true, true, null, markup);
            return message;
        }

        private string CreateRaidText(RaidParticipation raid)
        {
            StringBuilder participationSB = new StringBuilder();
            CalculateParticipationBlock(raid, participationSB, out string tps);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Subscribed")) + $":</b> {tps}");
            if (!string.IsNullOrWhiteSpace(raid.Raid.Remarks))
            {
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Remarks")) + $":</b> {MessageUtils.HtmlEscape(raid.Raid.Remarks)}");
            }
            if (!string.IsNullOrWhiteSpace(raid.Raid.Raid))
            {
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Raid")) + $":</b> {MessageUtils.HtmlEscape(raid.Raid.Raid)}");
            }
            if (!string.IsNullOrWhiteSpace(raid.Raid.Gym))
            {
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Gym")) + $":</b> {MessageUtils.HtmlEscape(raid.Raid.Gym)}");
            }
            if (raid.Raid.Alignment != Team.Unknown)
            {
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Alignment")) + $":</b> {MessageUtils.HtmlEscape(raid.Raid.Alignment.AsReadableString())}");
            }
            if (!string.IsNullOrWhiteSpace(raid.Raid.Address))
            {
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Address")) + $":</b> {MessageUtils.HtmlEscape(raid.Raid.Address)}");
            }

            if (null != raid.Raid.Location)
            {
                string lat = raid.Raid.Location.Latitude.ToString(CultureInfo.InvariantCulture);
                string lon = raid.Raid.Location.Longitude.ToString(CultureInfo.InvariantCulture);
                string externalurls = "";
                if (raid.Raid.Sources != null)
                {
                    foreach (var source in raid.Raid.Sources.Where(x => !string.IsNullOrEmpty(x.URL)).ToArray())
                    {
                        externalurls += $"<a href=\"{source.URL}\">" + _HTML_(source.SourceID) + $"</a>, ";
                    }
                }
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Links")) + $":</b> ({externalurls}<a href=\"https://www.google.com/maps/?daddr={lat},{lon}\">" + _HTML_(I18N.GetString("route")) + $"</a>, <a href=\"https://ingress.com/intel?ll={lat},{lon}&z=17\">" + _HTML_(I18N.GetString("portal map")) + $"</a>)");
            }

            if (raid.Raid.RaidUnlockTime != default(DateTime) && raid.Raid.RaidUnlockTime >= DateTime.UtcNow)
            {
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Hatches in")) + $":</b> {TimeService.AsShortTime(raid.Raid.RaidUnlockTime)} (over {TimeService.AsReadableTimespan(raid.Raid.RaidUnlockTime - DateTime.UtcNow)})");
            }

            if (raid.Raid.RaidEndTime == default(DateTime) || raid.Raid.RaidEndTime < DateTime.UtcNow)
            {
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Time")) + $":</b> Unknown or in the past");
            }
            else
            {
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Ends")) + $":</b> {TimeService.AsShortTime(raid.Raid.RaidEndTime)} (" + _HTML_(I18N.GetString("in")) + $" {TimeService.AsReadableTimespan(raid.Raid.RaidEndTime - DateTime.UtcNow)})");
            }

            sb.Append(participationSB);

            var naySayers = raid.Rejected.Select(x => x).OrderBy(x => x.ShortName());
            if (naySayers.Any())
            {
                sb.AppendLine($"");
                var str = string.Join(", ", naySayers.Select(x => $"{x.ShortName()}"));
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Declined")) + $":</b> {str}");
            }

            var userSettingsCollection = DB.GetCollection<UserSettings>();
            var undecided = raid.Maybe.Select(x => x).OrderBy(x => x.ShortName());
            if (undecided.Any())
            {
                sb.AppendLine($"");
                var str = string.Join(", ", undecided.Select(x =>
                {
                    string name = x.ShortName().TrimStart('@');
                    var userRecord = userSettingsCollection.Find(y => y.User.ID == x.ID).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(userRecord?.Alias))
                    {
                        name = userRecord.Alias;
                    }
                    if (userRecord?.Level > 0)
                    {
                        name += $" (" + _HTML_(I18N.GetString("level")) + $" {userRecord?.Level})";
                    }
                    return name;
                }));
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Maybe")) + $":</b> {str}");
            }

            var alreadyDone = raid.Done.Select(x => x).OrderBy(x => x.ShortName());
            if (alreadyDone.Any())
            {
                sb.AppendLine($"");
                var str = string.Join(", ", alreadyDone.Select(x => $"{x.ShortName()}"));
                sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Done")) + $":</b> {str}");
            }

            //sb.AppendLine($"\n<i>" + _HTML_(I18N.GetString("In a private chat, use the /level command to set your player level.")) + $"</i>");

            sb.AppendLine($"\n#raid updated: <i>{TimeService.AsFullTime(DateTime.UtcNow)}</i>");
            return sb.ToString();
        }

        private void CalculateParticipationBlock(RaidParticipation raid, StringBuilder sb, out string tps)
        {
            var userSettingsCollection = DB.GetCollection<UserSettings>();
            List<string> tpsElements = new List<string>();
            tps = "";
            int counter = 0;
            foreach (Team team in raid.Participants.Keys.OrderBy(x => x))
            {
                var participants = raid.Participants[team];
                if (participants.Count > 0)
                {
                    sb.AppendLine($"");
                    sb.AppendLine($"<b>{team.AsReadableString()} ({participants.Select(x => 1 + x.Extra).Sum()}):</b>");
                    foreach (var p in participants.OrderBy(x => x.User.ShortName()))
                    {
                        string name = p.User.ShortName().TrimStart('@');
                        var userRecord = userSettingsCollection.Find(x => x.User.ID == p.User.ID).FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(userRecord?.Alias))
                        {
                            name = userRecord.Alias;
                        }
                        if (userRecord?.Level > 0)
                        {
                            name += $" (" + _HTML_(I18N.GetString("level")) + $" {userRecord?.Level})";
                        }
                        sb.Append($"  - {MessageUtils.HtmlEscape(name)}");

                        if (p.Extra > 0)
                        {
                            counter += p.Extra;
                            sb.Append($" +{p.Extra}");
                        }
                        else
                        {
                            sb.Append($"");
                        }

                        if (p.UtcArrived != default(DateTime))
                        {
                            sb.Append(" [" + _HTML_(I18N.GetPluralString("there for {0}", "there for {0}", p.Extra + 1, TimeService.AsReadableTimespan(DateTime.UtcNow - p.UtcArrived))) + "]");
                        }
                        else if (p.UtcWhen != default(DateTime))
                        {
                            sb.Append($" [{TimeService.AsShortTime(p.UtcWhen)}]");
                        }
                        else
                        {
                            sb.Append($"");
                        }

                        sb.AppendLine();
                    }
                }
                tpsElements.Add($"{participants.Sum(x => 1 + x.Extra)}{team.AsIcon()}");
                counter += participants.Count;
            }

            var tpsSub = string.Join(" ", tpsElements);
            tps = $"{counter} ({tpsSub})";

            //Removed, alredy summarised at the top of the message
            //if (counter > 0)
            //{
            //    sb.AppendLine($"");
            //    sb.AppendLine($"<b>" + _HTML_(I18N.GetString("Subscriptions")) + $":</b> " + _HTML_(I18N.GetPluralString("{0} player", "{0} players", counter, counter)));
            //}
        }

        private InlineKeyboardMarkup CreateMarkupFor(RaidParticipation raid)
        {
            if (raid.Raid.RaidEndTime <= DateTime.UtcNow)
            {
                return null; // no buttons
            }
            else
            {
                InlineKeyboardMarkup result = new InlineKeyboardMarkup { inline_keyboard = new List<List<InlineKeyboardButton>>() };
                string shareString = $"{IqPrefix}{raid.PublicID}";

                List<InlineKeyboardButton> row;

                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("Yes")), callback_data = $"{QrJoin}:{raid.PublicID}:0" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("❤️")), callback_data = $"{QrJoin}:{raid.PublicID}::{(int)Team.Valor}" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("💙")), callback_data = $"{QrJoin}:{raid.PublicID}::{(int)Team.Mystic}" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("💛")), callback_data = $"{QrJoin}:{raid.PublicID}::{(int)Team.Instinct}" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("No")), callback_data = $"{QrDecline}:{raid.PublicID}" });
                result.inline_keyboard.Add(row);

                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("I +1")), callback_data = $"{QrJoin}:{raid.PublicID}:1" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("I +2")), callback_data = $"{QrJoin}:{raid.PublicID}:2" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("I +3")), callback_data = $"{QrJoin}:{raid.PublicID}:3" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("I +4")), callback_data = $"{QrJoin}:{raid.PublicID}:4" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("I +5")), callback_data = $"{QrJoin}:{raid.PublicID}:5" });
                result.inline_keyboard.Add(row);

                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("🔄")), callback_data = $"{QrRefresh}:{raid.PublicID}" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("✏️")), callback_data = $"{QrEdit}:{raid.PublicID}" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("Maybe")), callback_data = $"{QrMaybe}:{raid.PublicID}" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("Done")), callback_data = $"{QrDone}:{raid.PublicID}" });
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("Share")), switch_inline_query = $"{shareString}" });
                result.inline_keyboard.Add(row);

                if (!raid.IsPublished)
                {
                    row = new List<InlineKeyboardButton>();
                    row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("📣 Publish")), callback_data = $"{QrPublish}:{raid.PublicID}" });
                    result.inline_keyboard.Add(row);
                }

                row = new List<InlineKeyboardButton>();
                var dtStart = DateTime.UtcNow;
                if (raid.Raid.RaidUnlockTime > dtStart) dtStart = raid.Raid.RaidUnlockTime;
                if (dtStart.Minute % 5 != 0)
                {
                    dtStart += TimeSpan.FromMinutes(5 - (dtStart.Minute % 5));
                }

                while (dtStart <= raid.Raid.RaidEndTime)
                {
                    row.Add(new InlineKeyboardButton { text = $"{TimeService.AsShortTime(dtStart)}", callback_data = $"{QrSetTime}:{raid.PublicID}:{dtStart.Ticks}" }); ;
                    dtStart += TimeSpan.FromMinutes(5);
                }
                row.Add(new InlineKeyboardButton { text = _HTML_(I18N.GetString("Arrived")), callback_data = $"{QrArrived}:{raid.PublicID}" }); ;
                var addnRows = SplitButtonsIntoLines(row, maxElementsPerLine: 5, maxCharactersPerLine: 30);

                result.inline_keyboard.AddRange(addnRows);
                return result;
            }
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
    }
}
