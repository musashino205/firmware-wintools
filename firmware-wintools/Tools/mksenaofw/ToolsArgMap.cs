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
			string tmp;

			for (int i = arg_idx; i < args.Length; i++)
			{
				if (!args[i].StartsWith("-"))
					continue;

				switch (args[i].Replace("-", ""))
				{
					case "t":
						if (Utils.GetStrParam(args, i, out tmp) &&
						    byte.TryParse(tmp, out byte conv_type))
						{
							subprops.fw_type = conv_type;
							i++;
						}
						break;
					case "v":
						if (Utils.GetStrParamOrKeep(args, i, ref subprops.version))
							i++;
						break;
					case "r":
						if (Utils.GetStrParam(args, i, out tmp) &&
						    Utils.StrToUInt(tmp, out uint conv_vendor,
							    NumberStyles.None))
						{
							subprops.vendor = conv_vendor;
							i++;
						}
						break;
					case "p":
						if (Utils.GetStrParam(args, i, out tmp) &&
						    Utils.StrToUInt(tmp, out uint conv_product,
							    NumberStyles.None))
						{
							subprops.product = conv_product;
							i++;
						}
						break;
					case "m":
						if (Utils.GetStrParam(args, i, out tmp) &&
						    Utils.StrToUInt(tmp, out uint conv_magic,
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
						if (Utils.GetStrParam(args, i, out tmp) &&
						    Utils.StrToInt(tmp, out int conv_bs,
							    NumberStyles.None))
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
