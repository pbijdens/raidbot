using Botje.Core.Utils;
using NGettext;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonRaidBot.Utils
{
    public class TimeService : ITimeService
    {
        [Inject]
        public ICatalog I18N { get; set; }

        /// <summary>
        /// Convert a datetime to justa short time.
        /// </summary>
        /// <param name="utc"></param>
        /// <returns></returns>
        public string AsShortTime(DateTime utc)
        {
            DateTime converted = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeUtils.TzInfo);
            return converted.ToString(I18N.GetString("HH:mm"));
        }

        public string AsFullTime(DateTime utc)
        {
            DateTime converted = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeUtils.TzInfo);
            return converted.ToString(I18N.GetString("HH:mm:ss.fff"));
        }

        /// <summary>
        /// Human readable timespan.
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public string AsReadableTimespan(TimeSpan ts)
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
            return String.Format(new LocalizedHMSFormatter(I18N), cutoff[cutoff.Keys[near]], ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
        }


        public string AsDutchString(DateTime dt)
        {
            string[] months = new string[] {
                I18N.GetString("January"),
                I18N.GetString("February"),
                I18N.GetString("March"),
                I18N.GetString("April"),
                I18N.GetString("May"),
                I18N.GetString("June"),
                I18N.GetString("July"),
                I18N.GetString("August"),
                I18N.GetString("September"),
                I18N.GetString("October"),
                I18N.GetString("November"),
                I18N.GetString("December")
            };

            return $"{dt.Day} {months[dt.Month - 1]} {dt.ToString(I18N.GetString("HH:mm"))}";
        }

        public string AsLocalShortTime(DateTime dt)
        {
            DateTime converted = TimeZoneInfo.ConvertTimeFromUtc(dt, TimeUtils.TzInfo);
            return dt.ToString(I18N.GetString("MM-dd-yy HH:mm"));
        }

        public class LocalizedHMSFormatter : ICustomFormatter, IFormatProvider
        {
            ICatalog I18N;

            public LocalizedHMSFormatter(ICatalog i18n)
            {
                I18N = i18n;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="format"></param>
            /// <param name="arg"></param>
            /// <param name="formatProvider"></param>
            /// <returns></returns>
            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                var TimeFormats = new Dictionary<string, string> {
                    {"S", "{0:P:"+I18N.GetString("seconds")+":"+I18N.GetString("second")+"}"},
                    {"M", "{0:P:"+I18N.GetString("minutes")+":"+I18N.GetString("minute")+"}"},
                    {"H","{0:P:"+I18N.GetString("hours")+":"+I18N.GetString("hour")+"}"},
                    {"D", "{0:P:"+I18N.GetString("days")+":"+I18N.GetString("day")+"}"}
                };

                return String.Format(new PluralFormatter(), TimeFormats[format], arg);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="formatType"></param>
            /// <returns></returns>
            public object GetFormat(Type formatType)
            {
                return formatType == typeof(ICustomFormatter) ? this : null;
            }
        }
    }
}
