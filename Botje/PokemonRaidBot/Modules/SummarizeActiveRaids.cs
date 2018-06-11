using Botje.Core;
using Botje.Core.Utils;
using Botje.DB;
using Botje.Messaging;
using NGettext;
using Ninject;
using PokemonRaidBot.Entities;
using PokemonRaidBot.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace PokemonRaidBot.Modules
{
    /// <summary>
    /// Module that removes published raids from the channel.
    /// </summary>
    public class SummarizeActiveRaids : IBotModule
    {
        public static bool NewRaidPosted = false;

        public static TimeSpan Interval = TimeSpan.FromSeconds(10);

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
        public ITimeService TimeService { get; set; }

        [Inject]
        public ICatalog I18N { get; set; }
        protected readonly Func<string, string> _HTML_ = (s) => MessageUtils.HtmlEscape(s);

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
            _log.Info($"Starting worker thread for {nameof(SummarizeActiveRaids)}");
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    if (DateTime.UtcNow.Hour >= 21 || DateTime.UtcNow.Hour < 4)
                    {
                        _log.Trace($"Skipping summary update cycle because the server rests.");
                        continue;
                    }

                    var publishedRaids = DB.GetCollection<RaidParticipation>().Find(x => x.Raid != null && x.Raid.RaidEndTime >= DateTime.UtcNow && x.Raid.RaidUnlockTime <= (DateTime.UtcNow + TimeSpan.FromHours(1)) && (x.IsPublished || x.Raid.Publications.Where(p => p.TelegramMessageID != default(long)).Any())).ToArray();

                    List<long> channels = new List<long> { Settings.PublicationChannel ?? 0 };
                    channels.AddRange(publishedRaids.SelectMany(r => r.Raid?.Publications?.Select(x => x.ChannelID) ?? new long[] { }));

                    foreach (var channel in channels.Distinct().ToArray())
                    {
                        StringBuilder message = new StringBuilder();
                        var raidsForChannel = publishedRaids.Where(x => ((channel == Settings.PublicationChannel) && x.IsPublished) || (x.Raid.Publications.Where(p => p.ChannelID == channel).Any())).ToArray();
                        foreach (var raid in raidsForChannel.OrderBy(x => x.Raid.RaidEndTime))
                        {
                            message.AppendLine($"{TimeService.AsShortTime(raid.Raid.RaidUnlockTime)}: <a href=\"http://pogoafo.nl/#{raid.Raid.Location.Latitude.ToString(CultureInfo.InvariantCulture)},{raid.Raid.Location.Longitude.ToString(CultureInfo.InvariantCulture)}\">{_HTML_(raid.Raid.Gym)}</a> - {_HTML_(raid.Raid.Raid)}: {raid.NumberOfParticipants()}");
                        }

                        var updateRecord = DB.GetCollection<ChannelUpdateMessage>().Find(x => x.ChannelID == channel).FirstOrDefault();
                        if (null == updateRecord)
                        {
                            updateRecord = new ChannelUpdateMessage
                            {
                                ChannelID = channel,
                                MessageID = long.MaxValue,
                                Hash = string.Empty
                            };
                            DB.GetCollection<ChannelUpdateMessage>().Insert(updateRecord);
                        }

                        var hash = HashUtils.CalculateSHA1Hash(message.ToString());
                        if (!string.Equals(hash, updateRecord.Hash) /*|| (DateTime.UtcNow - updateRecord.LastModificationDate > TimeSpan.FromSeconds(60))*/)
                        {
                            if (NewRaidPosted || updateRecord.MessageID == long.MaxValue)
                            {
                                NewRaidPosted = false;

                                // A new message was posted or the summary was never posted yet, delete the current message and posty a new summary
                                if (updateRecord.MessageID != long.MaxValue)
                                {
                                    try
                                    {
                                        Client.DeleteMessage(channel, updateRecord.MessageID);
                                        updateRecord.MessageID = long.MaxValue;
                                        updateRecord.Hash = hash;
                                    }
                                    catch (Exception ex)
                                    {
                                        _log.Warn(ex, $"Could not delete summary-message {updateRecord.MessageID} from channel {channel}");
                                    }
                                }

                                try
                                {
                                    var postedMessage = Client.SendMessageToChat(channel, message.ToString(), "HTML", true, true, null, null);
                                    if (null != postedMessage)
                                    {
                                        updateRecord.MessageID = postedMessage.MessageID;
                                    }
                                    else
                                    {
                                        _log.Warn($"Could not post summary-message to channel {channel} - null reply");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _log.Warn(ex, $"Could not post summary-message to channel {channel}");
                                }
                            }
                            else if (updateRecord.MessageID != long.MaxValue)
                            {
                                // There is no new raid posted, so update the current one
                                Client.EditMessageText($"{channel}", updateRecord.MessageID, null, message.ToString(), "HTML", true, null, "channel");
                                updateRecord.Hash = hash;
                            }
                        }

                        updateRecord.LastModificationDate = DateTime.UtcNow;
                        DB.GetCollection<ChannelUpdateMessage>().Update(updateRecord);
                    }
                }
                catch (ThreadAbortException)
                {
                    _log.Info($"Abort requested for thread.");
                    break;
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error in SummarizeActiveRaids thread. Ignoring.");
                }
                finally
                {
                    Thread.Sleep(Interval);
                }
            }
            _log.Info($"Stopped worker thread for {nameof(SummarizeActiveRaids)}");
        }
    }
}
