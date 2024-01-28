namespace LDAP_Connector.Helpers
{
	public static class StringHelper
	{
		public static string FormatCn(this string str)
		{
			return (str ?? string.Empty).Replace("\\,", ",").Replace(",", "\\,");
		}

		public static string NormalizeString(this string str)
		{
			return (str ?? string.Empty).Trim().ToLower();
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
	}
}
