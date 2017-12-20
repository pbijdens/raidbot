using Botje.Messaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Botje.Messaging.Telegram
{

    /// <summary>
    /// If we know the chat, then we throttle the edits for messages in that chat. Otherwise it's done on message ID.
    /// 
    /// If the chat is a channel, then we really really really don't want to update too often.
    /// 
    /// Limits are configures as:
    /// - number of requests allowed per time period
    /// - minimum delay between two consecutive reuests
    /// 
    /// For private chat and regular chats, the limits are roughly 10 messages per second no more often than once every 0.1 seconds
    /// For channels the limit is 1 message per 5 seconds
    /// 
    /// When multiple edits arrive during a blocked period, then all only the last edit is performed (the others are pointless anyway)
    /// 
    /// TODO TODO TODO:
    /// - CLEAN UP THE LIST WE NEVER DELETE ANY DATA :-)
    /// </summary>
    public class ThrottlingTelegramClient : TelegramClient
    {
        private class QueueData
        {
            public object Lock = new object();
            public TimeSpan Period = TimeSpan.FromSeconds(1);
            public int AllowedPerPeriod = 1;
            public Action Action;
            public List<DateTime> DequeueTimes = new List<DateTime>();
            public TimeSpan MinDelay;
        }

        private object _queueLock = new object();
        private Dictionary<string, QueueData> _queues = new Dictionary<string, QueueData>();

        public override void Start()
        {
            Thread t = new Thread(() =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        foreach (var kvp in _queues.Where(x => x.Value.Action != null).ToArray())
                        {
                            Action action = null;
                            lock (_queueLock)
                            {
                                // number of actions per specified period, including this one
                                kvp.Value.DequeueTimes.RemoveAll(x => x < DateTime.UtcNow - kvp.Value.Period);
                                var count = kvp.Value.DequeueTimes.Count() + 1;

                                // last action (these are sorted)
                                var last = kvp.Value.DequeueTimes.LastOrDefault();

                                // check that we don't exceed the limit, and that the time since the last meaasge
                                // is sufficient
                                if (count <= kvp.Value.AllowedPerPeriod && (last == null || DateTime.UtcNow - last >= kvp.Value.MinDelay))
                                {
                                    Log.Trace($"Allowing action with ID {kvp.Key} because {count} per {kvp.Value.Period} is allowed");

                                    // allow the action
                                    action = kvp.Value.Action;
                                    kvp.Value.Action = null;
                                    kvp.Value.DequeueTimes.Add(DateTime.UtcNow);
                                }
                                else
                                {
                                    Log.Trace($"Delaying action with ID {kvp.Key}");
                                }
                            }
                            if (null != action)
                            {
                                new Task(action).Start();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Could not de queue action...");
                    }
                    finally
                    {
                        Thread.Sleep(50);
                    }
                }
            })
            { IsBackground = true };
            t.Start();
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        private CancellationToken _cancellationToken;
        public override void Setup(string key, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            base.Setup(key, cancellationToken);
        }

        public override void EditMessageText(string chatID, long? messageID, string inlineMessageID, string text, string parseMode = null, bool? disableWebPagePreview = null, InlineKeyboardMarkup replyMarkup = null, string chatType = "private")
        {
            string key = $"{chatID}:{inlineMessageID}";

            int allowed = 3;
            var period = TimeSpan.FromSeconds(1);
            var minDelay = TimeSpan.FromSeconds(0.1);

            switch (chatType)
            {
                case "channel":
                    allowed = 1;
                    period = TimeSpan.FromSeconds(5);
                    minDelay = TimeSpan.FromSeconds(1);
                    break;
            }

            EnqueueActionForChat(key, () => base.EditMessageText(chatID, messageID, inlineMessageID, text, parseMode, disableWebPagePreview, replyMarkup), period, allowed, minDelay);
        }

        private void EnqueueActionForChat(string key, Action action, TimeSpan period, int allowed, TimeSpan minDelay)
        {
            lock (_queueLock)
            {
                if (!_queues.ContainsKey(key))
                {
                    var qd = new QueueData();
                    qd.Period = period;
                    qd.AllowedPerPeriod = allowed;
                    qd.MinDelay = minDelay;
                    _queues.Add(key, qd);
                }

                _queues[key].Action = action;
            }
        }
    }
}
