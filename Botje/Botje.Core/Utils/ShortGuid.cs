using System;

namespace Botje.Core.Utils
{
    /// <summary>
    /// Wrapper for the GUID class, but with a different parse and tostring method.
    /// </summary>
    public class ShortGuid
    {
        private const string GuidCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_"; // 0-63 [111111]

        private Guid _guid = default(Guid);

        public static ShortGuid NewGuid() => new ShortGuid { _guid = Guid.NewGuid() };

        public static ShortGuid Empty => new ShortGuid { _guid = Guid.Empty };

        public Guid Guid => _guid;

        public ShortGuid()
        {
            _guid = Guid.Empty;
        }

        public ShortGuid(Guid g)
        {
            _guid = g;
        }

        public static ShortGuid Parse(string s)
        {
            if (s.Length != 22) throw new FormatException($"{s} is not a valid short GUID");
            byte[] guid = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                guid[i] = (byte)GuidCharacters.IndexOf(s[i]);
            }
            byte b;
            b = (byte)GuidCharacters.IndexOf(s[16]);
            guid[0] |= (byte)((b & 48) << 2);
            guid[1] |= (byte)((b & 12) << 4);
            guid[2] |= (byte)((b & 3) << 6);
            b = (byte)GuidCharacters.IndexOf(s[17]);
            guid[3] |= (byte)((b & 48) << 2);
            guid[4] |= (byte)((b & 12) << 4);
            guid[5] |= (byte)((b & 3) << 6);
            b = (byte)GuidCharacters.IndexOf(s[18]);
            guid[6] |= (byte)((b & 48) << 2);
            guid[7] |= (byte)((b & 12) << 4);
            guid[8] |= (byte)((b & 3) << 6);
            b = (byte)GuidCharacters.IndexOf(s[19]);
            guid[9] |= (byte)((b & 48) << 2);
            guid[10] |= (byte)((b & 12) << 4);
            guid[11] |= (byte)((b & 3) << 6);
            b = (byte)GuidCharacters.IndexOf(s[20]);
            guid[12] |= (byte)((b & 48) << 2);
            guid[13] |= (byte)((b & 12) << 4);
            guid[14] |= (byte)((b & 3) << 6);
            b = (byte)GuidCharacters.IndexOf(s[21]);
            guid[15] |= (byte)((b & 48) << 2);

            return new ShortGuid(new Guid(guid));
        }

        public override string ToString()
        {
            char[] result = new char[22];
            byte[] bytes = _guid.ToByteArray();
            for (int i = 0; i < 16; i++)
            {
                byte index = (byte)(bytes[i] & 0x3F);
                result[i] = GuidCharacters[index];
                bytes[i] = (byte)(bytes[i] >> 6);
            }
            result[16] = GuidCharacters[(bytes[0] << 4) | (bytes[1] << 2) | bytes[2]];
            result[17] = GuidCharacters[(bytes[3] << 4) | (bytes[4] << 2) | bytes[5]];
            result[18] = GuidCharacters[(bytes[6] << 4) | (bytes[7] << 2) | bytes[8]];
            result[19] = GuidCharacters[(bytes[9] << 4) | (bytes[10] << 2) | bytes[11]];
            result[20] = GuidCharacters[(bytes[12] << 4) | (bytes[13] << 2) | bytes[14]];
            result[21] = GuidCharacters[(bytes[15] << 4)];

            return new string(result);
        }
    }
}
