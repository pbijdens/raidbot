using System;
using System.Collections.Concurrent;

namespace PokemonRaidBot.Utils
{
    public class RateCounter
    {
        private ConcurrentQueue<DateTime> _queue = new ConcurrentQueue<DateTime>();
        private readonly TimeSpan _period;

        public int Count => GetCount();

        public RateCounter(TimeSpan period)
        {
            _period = period;
        }

        private int GetCount()
        {
            while (_queue.Count > 0)
            {
                if (!_queue.TryPeek(out DateTime peek) || DateTime.UtcNow - peek < _period) break;
                _queue.TryDequeue(out _);
            }
            return _queue.Count;
        }

        public void Register()
        {
            _queue.Enqueue(DateTime.UtcNow);
        }
    }
}
