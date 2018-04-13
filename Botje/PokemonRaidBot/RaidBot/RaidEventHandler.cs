using Botje.Core;
using Botje.Core.Utils;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.Events;
using Botje.Messaging.Models;
using Botje.Messaging.PrivateConversation;
using Ninject;
using PokemonRaidBot.RaidBot.Entities;
using PokemonRaidBot.RaidBot.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PokemonRaidBot.RaidBot
{
    public class RaidEventHandler : IBotModule
    {
        private ILogger _log;
        private object _raidLock = new object(); // TODO: too broad, should be per raid but it'll do for now

        private const string QrJoin = "qr.joi"; // qr.joi:{raid}:{extra}:{team}
        private const string QrDecline = "qr.dec"; // qr.dec:{raid}
        private const string QrRefresh = "qr.ref"; // qr.ref:{raid}
        private const string QrPublish = "qr.pub"; // qr.pub:{raid}
        private const string QrSetTime = "qr.sti"; // qr.sti:{raid}:{ticks}
        private const string QrArrived = "qr.arr"; // qr.aee:{raid}
        private const string QrSetAlignment = "qr.cco"; // qr.cco:{raid}
        private const string QrAlignmentSelected = "qr.als"; // qr.als:{raid}:{teamid}
        private const string IqPrefix = "qr-";

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
                        title = $"{raid.Raid.Raid} {TimeUtils.AsShortTime(raid.Raid.RaidUnlockTime)}-{TimeUtils.AsShortTime(raid.Raid.RaidEndTime)}",
                        description = $"{raid.Raid.Raid} raid bij {raid.Raid.Gym} {raid.Raid.Address}",
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
                    _log.Info($"{e.CallbackQuery.From.DisplayName()} has arrived for raid {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, $"Je bent er! Geweldig!");
                    UpdateUserRaidJoinOrUpdateAttendance(e.CallbackQuery.From, args[0]);
                    UpdateUserRaidArrived(e.CallbackQuery.From, args[0]);
                    UpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrDecline: // :raid
                    _log.Info($"{e.CallbackQuery.From.DisplayName()} has declined for raid {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, $"Jammer 😞");
                    UpdateUserRaidNegative(e.CallbackQuery.From, args[0]);
                    UpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrJoin: // :raid:extra:team
                    _log.Info($"{e.CallbackQuery.From.DisplayName()} will join raid {args[0]} [{e.CallbackQuery.Data}]");
                    if (args.Length >= 3 && int.TryParse(args[2], out int teamID) && teamID >= (int)Team.Unknown && teamID <= (int)Team.Instinct)
                    {
                        _log.Info($"{e.CallbackQuery.From.DisplayName()} joined team {((Team)teamID).AsReadableString()}");
                        Client.AnswerCallbackQuery(e.CallbackQuery.ID, $"Ingeschreven voor voor team {((Team)teamID).AsReadableString()}");
                        UpdateUserSettingsForTeam(e.CallbackQuery.From, (Team)teamID);
                    }
                    else
                    {
                        Client.AnswerCallbackQuery(e.CallbackQuery.ID, $"Inschrijving geregeld.");
                    }
                    // Because we updated the user settings first, the user will automatically be added or moved to the
                    // correct team by the join function.
                    UpdateUserRaidJoinOrUpdateAttendance(e.CallbackQuery.From, args[0]);
                    if (args.Length >= 2 && int.TryParse(args[1], out int extra) && extra >= 0)
                    {
                        UpdateUserRaidExtra(e.CallbackQuery.From, args[0], extra);
                    }
                    UpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrPublish: // :raid
                    _log.Info($"{e.CallbackQuery.From.DisplayName()} published raid {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, $"Raid wordt gepubliceerd.");
                    PublishRaid(e.CallbackQuery.From, args[0]);
                    UpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrRefresh: // :raid
                    _log.Info($"{e.CallbackQuery.From.DisplayName()} refreshed {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, $"Raid informatie wordt ververst...");
                    UpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrSetTime: // :raid:ticks
                    UpdateUserRaidJoinOrUpdateAttendance(e.CallbackQuery.From, args[0]);
                    if (long.TryParse(args[1], out long ticks))
                    {
                        var utcWhen = new DateTime(ticks);
                        _log.Info($"{e.CallbackQuery.From.DisplayName()} updated their time for raid {args[0]} to {TimeUtils.AsShortTime(utcWhen)}");
                        Client.AnswerCallbackQuery(e.CallbackQuery.ID, $"Je bent er om {TimeUtils.AsShortTime(utcWhen)}.");
                        UpdateUserRaidTime(e.CallbackQuery.From, args[0], utcWhen);
                    }
                    else
                    {
                        Client.AnswerCallbackQuery(e.CallbackQuery.ID, $"Tijd wordt niet aangepast want ik begrijp het niet.");
                    }
                    UpdateRaidMessage(e.CallbackQuery.Message?.Chat?.ID, e.CallbackQuery.Message?.MessageID, e.CallbackQuery.InlineMessageId, args[0], e.CallbackQuery.Message?.Chat?.Type);
                    break;
                case QrSetAlignment: // :{raid}
                    _log.Info($"{e.CallbackQuery.From.DisplayName()} wants to change the gym alignment {args[0]}");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, $"Pas de kleur aan in je privé-chat met de bot.");
                    try
                    {
                        Client.SendMessageToChat(e.CallbackQuery.From.ID, $"Welke kleur heeft de gym op dit moment?", "HTML", true, true, null, CreateAlignmentMenu(args[0]));
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"User {e.CallbackQuery.From.ID} tried to change the gym-alignment but couldn't becasue of error {ex.GetType().Name} - {ex.Message}.");
                    }
                    break;
                case QrAlignmentSelected: // :{raid}:{alignment}
                    var team = (Team)Int32.Parse(args[1]);
                    _log.Info($"{e.CallbackQuery.From.DisplayName()} changed the gym alignment for {args[0]} to {team}");
                    Client.EditMessageText($"{e.CallbackQuery.Message.Chat.ID}", e.CallbackQuery.Message.MessageID, null, "Gym kleur is aangepast. Je moet zelf even de gepubliceerde raid verversen.");
                    Client.AnswerCallbackQuery(e.CallbackQuery.ID, $"Dank je wel. Ververs zelf even de gepubliceerde raid.");
                    UpdateGymAlignment(e.CallbackQuery.From, args[0], team);
                    break;
            }
        }

        private void UpdateGymAlignment(User from, string raidPublicID, Team team)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (_raidLock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidPublicID).First();
                if (null != raid)
                {
                    raid.Raid.Alignment = team;
                    raidCollection.Update(raid);
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

        private void UpdateUserRaidJoinOrUpdateAttendance(User user, string raidID)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (_raidLock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = GetOrCreateUserSettings(user, out _);
                raid.Rejected.RemoveAll(x => x.ID == user.ID);
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

        private void UpdateUserRaidNegative(User user, string raidID)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (_raidLock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = GetOrCreateUserSettings(user, out _);
                raid.Rejected.RemoveAll(x => x.ID == user.ID);
                raid.Participants.ToList().ForEach(kvp =>
                {
                    kvp.Value.RemoveAll(x => x.User.ID == user.ID);
                });
                raid.Rejected.Add(user);
                raid.Rejected.Sort((x, y) => string.Compare(x.DisplayName(), y.DisplayName()));
                raidCollection.Update(raid);
            }
        }

        private void UpdateUserRaidArrived(User user, string raidID)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            lock (_raidLock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = GetOrCreateUserSettings(user, out _);
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
            lock (_raidLock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = GetOrCreateUserSettings(user, out _);
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
            lock (_raidLock)
            {
                var raid = raidCollection.Find(x => x.PublicID == raidID).First();
                var userSettings = GetOrCreateUserSettings(user, out _);
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
            var userSettings = GetOrCreateUserSettings(user, out var userSettingsCollection);
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

                ShareRaidToChat(raid, Settings.PublicationChannel.Value);
            }
        }

        private void UpdateRaidMessage(long? chatID, long? messageID, string inlineMessageId, string raidID, string chatType)
        {
            var raidCollection = DB.GetCollection<RaidParticipation>();
            var raid = raidCollection.Find(x => x.PublicID == raidID).FirstOrDefault();
            if (null == raid)
            {
                _log.Error($"Got update request for raid {raidID}, but I cánt find it");
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

        private void ShareRaidToChat(RaidParticipation raid, long chatID)
        {
            string text = CreateRaidText(raid);
            InlineKeyboardMarkup markup = CreateMarkupFor(raid);
            Client.SendMessageToChat(chatID, text, "HTML", true, true, null, markup);
        }

        private string CreateRaidText(RaidParticipation raid)
        {
            StringBuilder participationSB = new StringBuilder();
            CalculateParticipationBlock(raid, participationSB, out string tps);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<b>Ingeschreven:</b> {tps}");
            if (!string.IsNullOrWhiteSpace(raid.Raid.Raid))
            {
                sb.AppendLine($"<b>Raid:</b> {MessageUtils.HtmlEscape(raid.Raid.Raid)}");
            }
            if (!string.IsNullOrWhiteSpace(raid.Raid.Gym))
            {
                sb.AppendLine($"<b>Gym:</b> {MessageUtils.HtmlEscape(raid.Raid.Gym)}");
            }
            if (raid.Raid.Alignment != Team.Unknown)
            {
                sb.AppendLine($"<b>Gym kleur:</b> {MessageUtils.HtmlEscape(raid.Raid.Alignment.AsReadableString())}");
            }
            if (!string.IsNullOrWhiteSpace(raid.Raid.Address))
            {
                sb.AppendLine($"<b>Adres:</b> {MessageUtils.HtmlEscape(raid.Raid.Address)}");
            }

            if (null != raid.Raid.Location)
            {
                string lat = raid.Raid.Location.Latitude.ToString(CultureInfo.InvariantCulture);
                string lon = raid.Raid.Location.Longitude.ToString(CultureInfo.InvariantCulture);
                sb.AppendLine($"<b>Links:</b> (<a href=\"https://www.google.com/maps/?daddr={lat},{lon}\">route</a>, <a href=\"https://ingress.com/intel?ll={lat},{lon}&z=17\">intel</a>)");
            }

            if (raid.Raid.RaidUnlockTime != default(DateTime) && raid.Raid.RaidUnlockTime >= DateTime.UtcNow)
            {
                sb.AppendLine($"<b>Komt uit:</b> {TimeUtils.AsShortTime(raid.Raid.RaidUnlockTime)} (over {TimeUtils.AsReadableTimespan(raid.Raid.RaidUnlockTime - DateTime.UtcNow)})");
            }

            if (raid.Raid.RaidEndTime == default(DateTime) || raid.Raid.RaidEndTime < DateTime.UtcNow)
            {
                sb.AppendLine($"<b>Tijd:</b> Onbekend of al voorbij");
            }
            else
            {
                sb.AppendLine($"<b>Afgelopen:</b> {TimeUtils.AsShortTime(raid.Raid.RaidEndTime)} (nog {TimeUtils.AsReadableTimespan(raid.Raid.RaidEndTime - DateTime.UtcNow)})");
            }

            sb.Append(participationSB);

            var naySayers = raid.Rejected.Select(x => x).OrderBy(x => x.ShortName());
            if (naySayers.Any())
            {
                sb.AppendLine($"");
                var str = string.Join(", ", naySayers.Select(x => $"{x.ShortName()}"));
                sb.AppendLine($"<b>Afgemeld:</b> {str}");
            }
            sb.AppendLine($"\r\n#raid updated: <i>{DateTime.UtcNow.AsFullTime()}</i>");
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
                            string iszijn = p.Extra >= 1 ? "zijn" : "is";
                            sb.Append($" [{iszijn} er al {TimeUtils.AsReadableTimespan(DateTime.UtcNow - p.UtcArrived)}]");
                        }
                        else if (p.UtcWhen != default(DateTime))
                        {
                            sb.Append($" [{TimeUtils.AsShortTime(p.UtcWhen)}]");
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

            if (counter > 0)
            {
                sb.AppendLine($"");
                string persoonpersonen = counter == 1 ? "persoon" : "personen";
                sb.AppendLine($"<b>Opkomst:</b> {counter} {persoonpersonen}");
            }
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
                row.Add(new InlineKeyboardButton { text = $"Ja!", callback_data = $"{QrJoin}:{raid.PublicID}:0" });
                row.Add(new InlineKeyboardButton { text = $"❤️", callback_data = $"{QrJoin}:{raid.PublicID}::{(int)Team.Valor}" });
                row.Add(new InlineKeyboardButton { text = $"💙", callback_data = $"{QrJoin}:{raid.PublicID}::{(int)Team.Mystic}" });
                row.Add(new InlineKeyboardButton { text = $"💛", callback_data = $"{QrJoin}:{raid.PublicID}::{(int)Team.Instinct}" });
                row.Add(new InlineKeyboardButton { text = $"Nee", callback_data = $"{QrDecline}:{raid.PublicID}" });
                result.inline_keyboard.Add(row);

                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton { text = $"Ik +1", callback_data = $"{QrJoin}:{raid.PublicID}:1" });
                row.Add(new InlineKeyboardButton { text = $"Ik +2", callback_data = $"{QrJoin}:{raid.PublicID}:2" });
                row.Add(new InlineKeyboardButton { text = $"Ik +3", callback_data = $"{QrJoin}:{raid.PublicID}:3" });
                row.Add(new InlineKeyboardButton { text = $"Ik +4", callback_data = $"{QrJoin}:{raid.PublicID}:4" });
                row.Add(new InlineKeyboardButton { text = $"Ik +5", callback_data = $"{QrJoin}:{raid.PublicID}:5" });
                result.inline_keyboard.Add(row);

                row = new List<InlineKeyboardButton>();
                row.Add(new InlineKeyboardButton { text = "🔄", callback_data = $"{QrRefresh}:{raid.PublicID}" });
                row.Add(new InlineKeyboardButton { text = "💟", callback_data = $"{QrSetAlignment}:{raid.PublicID}" });
                row.Add(new InlineKeyboardButton { text = "Delen", switch_inline_query = $"{shareString}" });
                if (!raid.IsPublished)
                {
                    row.Add(new InlineKeyboardButton { text = $"📣 Publiceren", callback_data = $"{QrPublish}:{raid.PublicID}" });
                }
                result.inline_keyboard.Add(row);

                row = new List<InlineKeyboardButton>();
                var dtStart = DateTime.UtcNow;
                if (raid.Raid.RaidUnlockTime > dtStart) dtStart = raid.Raid.RaidUnlockTime;
                if (dtStart.Minute % 5 != 0)
                {
                    dtStart += TimeSpan.FromMinutes(5 - (dtStart.Minute % 5));
                }

                while (dtStart <= raid.Raid.RaidEndTime)
                {
                    row.Add(new InlineKeyboardButton { text = $"{TimeUtils.AsShortTime(dtStart)}", callback_data = $"{QrSetTime}:{raid.PublicID}:{dtStart.Ticks}" }); ;
                    dtStart += TimeSpan.FromMinutes(5);
                }
                row.Add(new InlineKeyboardButton { text = $"Ben er", callback_data = $"{QrArrived}:{raid.PublicID}" }); ;
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
