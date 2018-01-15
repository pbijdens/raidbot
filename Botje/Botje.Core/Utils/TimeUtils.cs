using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Botje.Core.Utils
{
    /// <summary>
    /// Helpers for working with time.
    /// </summary>
    public static class TimeUtils
    {
        private static TimeZoneInfo _tzInfo;

        static TimeUtils()
        {
            Initialize(new string[] { "UTC" });
        }

        /// <summary>
        /// Initialize to these timezones. Note that windows timzeones are different form Unix/Linux ones! Give this method multiple choices and the first one that exists will be used.
        /// </summary>
        /// <param name="timezones"></param>
        public static void Initialize(string[] timezones)
        {
            _tzInfo = null;
            foreach (var tzID in timezones)
            {
                _tzInfo = TimeZoneInfo.GetSystemTimeZones().Where(x => x.Id == tzID).FirstOrDefault();
                if (null != _tzInfo)
                {
                    return;
                }
            }
            if (null == _tzInfo)
            {
                string tzstr = string.Join(", ", TimeZoneInfo.GetSystemTimeZones().OrderBy(x => x.Id).Select(x => $"'{x.Id}'").ToArray());
                Debug.WriteLine($"Timezone not found, pick one of these: {tzstr}");
            }
        }

        /// <summary>
        /// Convert a datetime to justa short time.
        /// </summary>
        /// <param name="utc"></param>
        /// <returns></returns>
        public static string AsShortTime(this DateTime utc)
        {
            DateTime converted = TimeZoneInfo.ConvertTimeFromUtc(utc, _tzInfo);
            return converted.ToString("HH:mm");
        }

        public static string AsFullTime(this DateTime utc)
        {
            DateTime converted = TimeZoneInfo.ConvertTimeFromUtc(utc, _tzInfo);
            return converted.ToString("HH:mm:ss.fff");
        }

        /// <summary>
        /// Human readable timespan.
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static string AsReadableTimespan(this TimeSpan ts)
        {
            // formats and its cutoffs based on totalseconds
            var cutoff = new SortedList<long, string> {
               {60, "{3:S}" },
               {60*60-1, "{2:M}, {3:S}"},
               {60*60, "{1:H}"},
               {24*60*60-1, "{1:H}, {2:M}"},
               {24*60*60, "{0:D}"},
               {Int64.MaxValue , "{0:D}, {1:H}"}
             };

            // find nearest best match
            var find = cutoff.Keys.ToList().BinarySearch((long)ts.TotalSeconds);

            // negative values indicate a nearest match
            var near = find < 0 ? Math.Abs(find) - 1 : find;

            // use custom formatter to get the string
            return String.Format(new HMSFormatter(), cutoff[cutoff.Keys[near]], ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
        }

        private static readonly string[] Maanden = new string[] { "januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december" };

        public static string AsDutchString(DateTime dt)
        {
            return $"{dt.Day} {Maanden[dt.Month - 1]} {dt.ToString("HH:mm")}";
        }

        public static TimeSpan ParseTimeSpan(string v)
        {
            TimeSpan result = TimeSpan.Zero;
            string number = "";
            foreach (char c in v)
            {
                if (char.IsDigit(c))
                {
                    number += c;
                }
                else
                {
                    switch (c)
                    {
                        case 'd':
                            result += TimeSpan.FromDays(Int32.Parse(number));
                            break;
                        case 'w':
                            result += TimeSpan.FromDays(7 * Int32.Parse(number));
                            break;
                        case 'h':
                        case 'u':
                            result += TimeSpan.FromHours(Int32.Parse(number));
                            break;
                    }
                    number = "";
                }
            }
            if (!string.IsNullOrWhiteSpace(number))
            {
                result += TimeSpan.FromDays(Int32.Parse(number));
            }
            return result;
        }

    }
}
