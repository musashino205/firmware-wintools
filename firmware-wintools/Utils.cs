using System.Globalization;

namespace firmware_wintools
{
	internal static class Utils
	{
		public static bool StrToLong(string valStr, out long cnv, NumberStyles styles)
		{
			int suf = 1;
			CultureInfo provider = CultureInfo.CurrentCulture;
			cnv = 0;

			if (valStr == null)
				return false;

			switch (styles)
			{
				case NumberStyles.None:     // use as "auto"
					/* jump to "default" if no hex prefix */
					if (!valStr.StartsWith("0x"))
						goto default;
					valStr = valStr.Remove(0, 2);
					styles = NumberStyles.HexNumber;
					break;
				case NumberStyles.HexNumber:    // do nothing
					break;
				default:
					styles = NumberStyles.Integer;
					break;
			}

			if (styles != NumberStyles.HexNumber)
			{
				switch (valStr[valStr.Length - 1])
				{
					case 'g':
					case 'G':
						suf *= 1024;
						goto case 'M';
					case 'm':
					case 'M':
						suf *= 1024;
						goto case 'K';
					case 'k':
					case 'K':
						suf *= 1024;
						valStr = valStr.Remove(valStr.Length - 1, 1);
						break;
				}
			}

			if (!long.TryParse(valStr, styles, provider, out cnv))
				return false;

			cnv *= suf;

			return true;
		}

		public static bool StrToInt(string val, out int cnv, NumberStyles numstyle)
		{
			cnv = 0;

			if (!StrToLong(val, out long cnvLong, numstyle) ||
				cnvLong > int.MaxValue ||
				cnvLong < int.MinValue)
				return false;

			cnv = (int)cnvLong;

			return true;
		}

		public static bool StrToUInt(string val, out uint cnv, NumberStyles numstyle)
		{
			cnv = 0;

			if (!StrToLong(val, out long cnvLong, numstyle) ||
				cnvLong > uint.MaxValue ||
				cnvLong < uint.MinValue)
				return false;

			cnv = (uint)cnvLong;

			return true;
		}

		public static bool StrToByteArray(ref string val, out byte[] cnv)
		{
			CultureInfo provider = CultureInfo.InvariantCulture;
			cnv = null;
			string c;

			if (val.Length % 2 != 0)
				return false;

			cnv = new byte[val.Length / 2];

			for (int i = 0; i < (val.Length / 2); i++)
			{
				c = val.Substring(i * 2, 2);
				if (!byte.TryParse(c, NumberStyles.HexNumber, provider, out cnv[i]))
				{
					val = c;
					return false;
				}
			}

			return true;
		}
	}
}
