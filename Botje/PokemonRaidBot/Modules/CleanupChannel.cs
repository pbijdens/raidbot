using Botje.Core;
using Botje.DB;
using Botje.Messaging;
using Ninject;
using System;
using System.Linq;
using System.Threading;

namespace PokemonRaidBot.Modules
{
    /// <summary>
    /// Module that removes published raids from the channel.
    /// </summary>
    public class CleanupChannel : IBotModule
    {
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
            _log.Info($"Starting worker thread for {nameof(CleanupChannel)}");
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var collection = DB.GetCollection<Entities.RaidParticipation>();
                    var raidList = collection.FindAll().ToArray();

                    // TODO: Delete raids that have been published automatically to other channels

                    var publishedRaidsForWhichMessagesMustBeDeleted = raidList.Where(x => x?.Raid.TelegramMessageID != null && x.Raid?.RaidEndTime <= DateTime.UtcNow).ToArray();
                    foreach (var rp in publishedRaidsForWhichMessagesMustBeDeleted.ToArray())
                    {
                        try
                        {
                            _log.Info($"Deleting message with ID {rp.Raid.TelegramMessageID.Value} from the raid channel {channelID} for raid {rp.PublicID} because the raid ended at {rp.Raid.RaidEndTime} (UTC) and it's {DateTime.UtcNow} (UTC) now.");
                            Client.DeleteMessage(channelID, rp.Raid.TelegramMessageID.Value);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, $"Error attempting to delete telegram message '{rp.Raid.TelegramMessageID}' from the channel for raid '{rp.PublicID}'");
                        }
                        finally
                        {
                            rp.Raid.TelegramMessageID = null;
                            collection.Update(rp);
                        }
                    }

                    var publishedRaidsThatNeedUpdating = raidList.Where(x => (x != null) && (x.Raid != null) && (x.LastRefresh < x.LastModificationTime) && (DateTime.UtcNow - x.LastRefresh > TimeSpan.FromSeconds(6)) && (x.Raid.TelegramMessageID != null));
                    foreach (var rp in publishedRaidsThatNeedUpdating.ToArray())
                    {
                        _log.Info($"Refreshing message {rp.PublicID} - last refresh {rp.LastRefresh} last edit {rp.LastModificationTime}");

                        rp.LastRefresh = DateTime.UtcNow;
                        collection.Update(rp);
                        RaidEventHandler.UpdateRaidMessage(channelID, rp.Raid.TelegramMessageID, null, rp.PublicID, "channel");
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
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
            _log.Info($"Stopped worker thread for {nameof(CleanupChannel)}");
        }
    }
}
