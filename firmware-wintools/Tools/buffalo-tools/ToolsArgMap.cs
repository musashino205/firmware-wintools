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
						if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.crypt_key) == 0)
							i++;
						break;
					case "m":
						if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.magic) == 0)
							i++;
						break;
					case "p":
						if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.product) == 0)
							i++;
						break;
					case "v":
						if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.version) == 0)
							i++;
						break;
					case "s":
						string seed = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref seed) == 0 &&
							byte.TryParse((seed.StartsWith("0x") ? seed.Replace("0x", "") : seed),
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
						string offset = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref offset) == 0 &&
							Utils.StrToInt(offset, out int conv_offset, 0))
						{
							subprops.offset = conv_offset;
							i++;
						}
						break;
					case "S":
						string size = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref size) == 0 &&
							Utils.StrToInt(size, out int conv_size, 0))
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
