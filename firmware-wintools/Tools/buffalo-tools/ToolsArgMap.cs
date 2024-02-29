using System;
using System.Globalization;

namespace firmware_wintools.Tools
{
	internal partial class Buffalo_Enc
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、mkedimaximgの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">mkedimaximgの機能プロパティ</param>
		public static void
		Init_args(string[] args, int arg_idx, ref Properties subprops)
		{
			string tmp;

			CultureInfo provider = CultureInfo.CurrentCulture;
			for (int i = arg_idx; i < args.Length; i++)
			{
				if (!args[i].StartsWith("-"))
					continue;

				switch (args[i].Replace("-", ""))
				{
					case "C":
						subprops.isMinorCksum = true;
						break;
					case "d":
						subprops.isde = true;
						break;
					case "l":
						subprops.islong = true;
						break;
					case "k":
						if (Utils.GetStrParam(args, i, out subprops.crypt_key))
							i++;
						break;
					case "m":
						if (Utils.GetStrParamOrKeep(args, i, ref subprops.magic))
							i++;
						break;
					case "p":
						if (Utils.GetStrParam(args, i, out subprops.product))
							i++;
						break;
					case "v":
						if (Utils.GetStrParam(args, i, out subprops.version))
							i++;
						break;
					case "s":
						if (Utils.GetStrParam(args, i, out tmp) &&
						    byte.TryParse((tmp.StartsWith("0x") ? tmp.Replace("0x", "") : tmp),
							    NumberStyles.HexNumber, provider, out byte conv_seed))
						{
							subprops.seed = conv_seed;
							i++;
						}
						break;
					case "F":
						subprops.force = true;
						break;
					case "O":
						if (Utils.GetStrParam(args, i, out tmp) &&
						    Utils.StrToInt(tmp, out int conv_offset,
							    NumberStyles.None))
						{
							subprops.offset = conv_offset;
							i++;
						}
						break;
					case "S":
						if (Utils.GetStrParam(args, i, out tmp) &&
						    Utils.StrToInt(tmp, out int conv_size,
							    NumberStyles.None))
						{
							subprops.size = conv_size;
							i++;
						}
						break;
				}
			}
		}
	}
}
