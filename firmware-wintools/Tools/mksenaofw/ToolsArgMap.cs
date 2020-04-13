using System;
using System.Globalization;

namespace firmware_wintools.Tools
{
	partial class ToolsArgMap
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、mksenaofwの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">mkedimaximgの機能プロパティ</param>
		public void Init_args_MkSenaoFw(string[] args, ref Tools.MkSenaoFw.Properties subprops)
		{
			CultureInfo provider = CultureInfo.CurrentCulture;
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
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
								Program.StrToUInt(vendor, out uint conv_vendor, 0) == 0)
							{
								subprops.vendor = conv_vendor;
								i++;
							}
							break;
						case "p":
							string product = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref product) == 0 &&
								Program.StrToUInt(product, out uint conv_product, 0) == 0)
							{
								subprops.product = conv_product;
								i++;
							}
							break;
						case "m":
							string magic = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref magic) == 0 &&
								Program.StrToUInt(magic, out uint conv_magic, 0) == 0)
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
								Int32.TryParse(bs, out int conv_bs))
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
}
