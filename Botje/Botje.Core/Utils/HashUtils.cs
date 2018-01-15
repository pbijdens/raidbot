using System;
using System.Security.Cryptography;
using System.Text;

namespace Botje.Core.Utils
{
    public static class HashUtils
    {
        /// <summary>
        /// Calculates the SHA1 hash for a string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The hash code, converted to a hexadecimal string.</returns>
        public static string CalculateSHA1Hash(String input)
        {
            using (var sha1 = new SHA1Managed())
            {
                Byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return HashToString(hash);
            }
        }

        /// <summary>
        /// Converts a hashcode to a string.
        /// </summary>
        /// <param name="hash">The hashcode.</param>
        /// <returns></returns>
        public static String HashToString(Byte[] hash)
        {
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
