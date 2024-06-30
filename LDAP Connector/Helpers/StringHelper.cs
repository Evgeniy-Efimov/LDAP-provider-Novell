namespace LDAP_Connector.Helpers
{
    public static class StringHelper
    {
        public static string FormatCnToWrite(this string str)
        {
            return (str ?? string.Empty)
                .Replace("\\,", ",")
                .Replace(",", "\\,")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();
        }

        public static string FormatDnToRead(this string str)
        {
            return (str ?? string.Empty)
                .Replace("\\,", "\\5c,")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("*", "\\2a")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();
        }

        public static string NormalizeString(this string str, bool toLower = true)
        {
            var result = (str ?? string.Empty).Trim().Replace(" ", string.Empty);

            result = toLower ? result.ToLower() : result.ToUpper();

            return result;
        }

        public static string GetSubstring(this string str, char endChar)
        {
            str = (str ?? string.Empty);
            var endCharIndex = str.IndexOf(endChar);

            if (endCharIndex >= 0)
            {
                return str.Substring(0, endCharIndex);
            }

            return str;
        }

        public static string GetSubstring(this string str, string startStr, string endStr)
        {
            str = (str ?? string.Empty);
            var startIndex = str.IndexOf(startStr);
            var endIndex = str.IndexOf(endStr);

            if (startIndex >= 0 && endIndex >= 0 && startIndex < endIndex)
            {
                return str.Substring(startIndex + startStr.Length, endIndex - (startIndex + startStr.Length));
            }

            return str;
        }

        public static string GetSubstring(this string str, string[] startStrs, string[] endStrs)
        {
            str = (str ?? string.Empty);

            var startStr = string.Empty;
            var endStr = string.Empty;

            foreach (var _startStr in startStrs)
            {
                if (str.IndexOf(_startStr) >= 0)
                {
                    startStr = _startStr;
                    break;
                }
            }

            foreach (var _endStr in endStrs)
            {
                if (str.IndexOf(_endStr) >= 0)
                {
                    endStr = _endStr;
                    break;
                }
            }

            return str.GetSubstring(startStr, endStr);
        }

        public static string ToSidString(this byte[] bytes)
        {
            var result = string.Empty;

            if (bytes == null || !bytes.Any())
                return result;

            try
            {
                //revision
                result += "S-" + bytes[0].ToString();

                //authority
                if (bytes[5] != 0 || bytes[6] != 0)
                {
                    result += "-" + string.Format(
                        "0x{0:2x}{1:2x}{2:2x}{3:2x}{4:2x}{5:2x}",
                        (short)bytes[1],
                        (short)bytes[2],
                        (short)bytes[3],
                        (short)bytes[4],
                        (short)bytes[5],
                        (short)bytes[6]);
                }
                else
                {
                    result += "-" + (
                        bytes[1] +
                        (bytes[2] << 8) +
                        (bytes[3] << 16) +
                        (bytes[4] << 24)
                        ).ToString();
                }

                //sub authority
                int authority_idx = 0;

                for (int i = 0; i < bytes[7]; i++)
                {
                    authority_idx = 8 + i * 4;
                    result += "-" + BitConverter.ToUInt32(bytes, authority_idx).ToString();
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Can't get SID value from bytes: {ex.GetFullErrorMessage()}");
            }
        }
    }
}
