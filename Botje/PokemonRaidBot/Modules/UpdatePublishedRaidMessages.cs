using Botje.Core;
using Botje.DB;
using Botje.Messaging;
using Ninject;
using PokemonRaidBot.Utils;
using System;
using System.Linq;
using System.Threading;

namespace PokemonRaidBot.Modules
{
    /// <summary>
    /// Module that removes published raids from the channel.
    /// </summary>
    public class UpdatePublishedRaidMessages : IBotModule
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

        private RateCounter _rateCounter05s = new RateCounter(TimeSpan.FromSeconds(0.5));
        private RateCounter _rateCounter1s = new RateCounter(TimeSpan.FromSeconds(1));
        private RateCounter _rateCounter2s = new RateCounter(TimeSpan.FromSeconds(2));
        private RateCounter _rateCounter3s = new RateCounter(TimeSpan.FromSeconds(3));
        private RateCounter _rateCounter6s = new RateCounter(TimeSpan.FromSeconds(6));

        private void Run()
        {
            var channelID = Settings.PublicationChannel.Value;
            _log.Info($"Starting worker thread for {nameof(UpdatePublishedRaidMessages)}");
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var collection = DB.GetCollection<Entities.RaidParticipation>();
                    var raidList = collection.FindAll().ToArray();

                    var candidateRaids = raidList.Where(x => (x != null) && (x.Raid != null) && (x.LastRefresh < x.LastModificationTime) && (x.Raid.TelegramMessageID != null));
                    var rp = candidateRaids.OrderBy(x => x.LastModificationTime).FirstOrDefault();

                    if (rp != null)
                    {
                        _log.Info($"> Raids that need updating: {candidateRaids.Count()}; Updates: 6={_rateCounter6s.Count}, 3={_rateCounter3s.Count}, 2={_rateCounter3s.Count}, 1={_rateCounter1s.Count}, 0.5={_rateCounter05s.Count}");

                        _log.Trace($"Refreshing message {rp.PublicID} - last refresh {rp.LastRefresh} last edit {rp.LastModificationTime}");

                        rp.LastRefresh = DateTime.UtcNow;
                        collection.Update(rp);

                        RaidEventHandler.UpdateRaidMessage(channelID, rp.Raid.TelegramMessageID, null, rp.PublicID, "channel");
                        _rateCounter05s.Register();
                        _rateCounter1s.Register();
                        _rateCounter2s.Register();
                        _rateCounter3s.Register();
                        _rateCounter6s.Register();

                        TimeSpan delay = TimeSpan.Zero;
                        if (_rateCounter6s.Count >= 7) delay = TimeSpan.FromSeconds(6); // try to average to one per second
                        else if (_rateCounter3s.Count >= 4) delay = TimeSpan.FromSeconds(3); // try to average to one per second
                        else if (_rateCounter2s.Count >= 3) delay = TimeSpan.FromSeconds(2); // try to average to one per second
                        else if (_rateCounter1s.Count >= 2) delay = TimeSpan.FromSeconds(1); // try to average to one per second
                        else if (_rateCounter05s.Count >= 0) delay = TimeSpan.FromSeconds(0.5); // try to average to one per second
                        else delay = TimeSpan.Zero;

                        candidateRaids = raidList.Where(x => (x != null) && (x.Raid != null) && (x.LastRefresh < x.LastModificationTime) && (x.Raid.TelegramMessageID != null));
                        _log.Info($"< Raids that need updating: {candidateRaids.Count()}; Updates: 6={_rateCounter6s.Count}, 3={_rateCounter3s.Count}, 2={_rateCounter3s.Count}, 1={_rateCounter1s.Count}, 0.5={_rateCounter05s.Count}, delay={delay}");

                        Thread.Sleep(delay);
                    }
                    else
                    {
                        // Nothing to do, sleep!
                        Thread.Sleep(TimeSpan.FromMilliseconds(500));
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
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                finally
                {
                }
            }
            _log.Info($"Stopped worker thread for {nameof(UpdatePublishedRaidMessages)}");
        }
    }
}
