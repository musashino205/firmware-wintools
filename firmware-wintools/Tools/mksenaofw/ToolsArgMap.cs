using System.Globalization;

namespace firmware_wintools.Tools
{
	internal partial class MkSenaoFw
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、mksenaofwの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">mkedimaximgの機能プロパティ</param>
		public static void
		Init_args(string[] args, int arg_idx, ref Properties subprops)
		{
			for (int i = arg_idx; i < args.Length; i++)
			{
				if (!args[i].StartsWith("-"))
					continue;

				switch (args[i].Replace("-", ""))
				{
					case "t":
						string type = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref type) == 0 &&
							byte.TryParse(type, out byte conv_type))
						{
							subprops.fw_type = conv_type;
							i++;
						}
						break;
					case "v":
						if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.version) == 0)
							i++;
						break;
					case "r":
						string vendor = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref vendor) == 0 &&
							Utils.StrToUInt(vendor, out uint conv_vendor,
									NumberStyles.None))
						{
							subprops.vendor = conv_vendor;
							i++;
						}
						break;
					case "p":
						string product = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref product) == 0 &&
							Utils.StrToUInt(product, out uint conv_product,
									NumberStyles.None))
						{
							subprops.product = conv_product;
							i++;
						}
						break;
					case "m":
						string magic = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref magic) == 0 &&
							Utils.StrToUInt(magic, out uint conv_magic,
									NumberStyles.None))
						{
							subprops.magic = conv_magic;
							i++;
						}
						break;
					case "z":
						subprops.pad = true;
						break;
					case "b":
						string bs = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref bs) == 0 &&
							Utils.StrToInt(bs, out int conv_bs, 0))
						{
							subprops.bs = conv_bs;
							i++;
						}
						break;
					case "d":
						subprops.isde = true;
						break;
				}
			}
		}
	}
}
