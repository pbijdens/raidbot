using System;
using System.Collections.Generic;

namespace Botje.Core.Utils
{
    /// <summary>
    /// HMS formatter.
    /// </summary>
    public class HMSFormatter : ICustomFormatter, IFormatProvider
    {
        // list of Formats, with a P customformat for pluralization
        static Dictionary<string, string> timeformats = new Dictionary<string, string> {
                {"S", "{0:P:seconden:seconde}"},
                {"M", "{0:P:minuten:minuut}"},
                {"H","{0:P:uur:uur}"},
                {"D", "{0:P:dagen:dag}"}
            };

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            return String.Format(new PluralFormatter(), timeformats[format], arg);
        }

        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }
    }
}
