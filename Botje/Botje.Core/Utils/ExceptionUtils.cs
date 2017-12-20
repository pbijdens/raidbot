using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace Botje.Core.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class ExceptionUtils
    {
        private static string[] HiddenProperties = new string[] { "Data", "InnerException" };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string AsString(Exception ex)
        {
            StringBuilder sb = new StringBuilder();

            int exceptionLevel = 0;
            Exception exception = ex;
            while (exception != null)
            {
                if (exceptionLevel == 0)
                    sb.AppendLine("[Exception]");
                else
                    sb.AppendLine(String.Format("\n[Inner exception level {0}]", exceptionLevel));

                sb.AppendLine(String.Format("Type = {0}", exception.GetType().Name));

                // Append public property values
                foreach (var property in exception.GetType().GetProperties())
                {
                    if (!HiddenProperties.Contains(property.Name))
                    {
                        object val = property.GetValue(exception, null);
                        if (val is Char || val is Byte)
                        {
                            sb.AppendLine(String.Format("HResult = 0x{0:X2} ({0})", val, val));
                        }
                        else if (val is Int16 || val is UInt16)
                        {
                            sb.AppendLine(String.Format("HResult = 0x{0:X4} ({0})", val, val));
                        }
                        else if (val is Int32 || val is UInt32)
                        {
                            sb.AppendLine(String.Format("HResult = 0x{0:X8} ({0})", val, val));
                        }
                        else if (val is Int64 || val is UInt64)
                        {
                            sb.AppendLine(String.Format("HResult = 0x{0:X16} ({0})", val, val));
                        }
                        else
                        {
                            sb.AppendFormat("{0} = {1}\n", property.Name, val);
                        }
                    }
                }

                if (exception.Data != null && exception.Data.Count > 0)
                {
                    sb.AppendLine("Data:");
                    foreach (DictionaryEntry kvp in exception.Data)
                    {
                        sb.AppendLine(String.Format("- {0} = {1}", kvp.Key, kvp.Value));
                    }
                }

                exception = exception.InnerException;
                exceptionLevel++;
            }
            return sb.ToString();
        }
    }
}
