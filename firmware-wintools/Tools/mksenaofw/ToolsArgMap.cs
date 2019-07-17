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
		public void Init_args_MkSenaoFw(string[] args, ref Tools.MkSenaoFw.Properties props)
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
								props.fw_type = conv_type;
								i++;
							}
							break;
						case "v":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref props.version) == 0)
								i++;
							break;
						case "r":
							string vendor = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref vendor) == 0 &&
								UInt32.TryParse((vendor.StartsWith("0x") ? vendor.Replace("0x", "") : vendor),
								NumberStyles.HexNumber, provider, out uint conv_vendor))
							{
								props.vendor = conv_vendor;
								i++;
							}
							break;
						case "p":
							string product = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref product) == 0 &&
								UInt32.TryParse((product.StartsWith("0x") ? product.Replace("0x", "") : product),
								NumberStyles.HexNumber, provider, out uint conv_product))
							{
								props.product = conv_product;
								i++;
							}
							break;
						case "m":
							string magic = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref magic) == 0 &&
								UInt32.TryParse((magic.StartsWith("0x") ? magic.Replace("0x", "") : magic),
								NumberStyles.HexNumber, provider, out uint conv_magic))
							{
								props.magic = conv_magic;
								i++;
							}
							break;
						case "z":
							props.pad = true;
							break;
						case "b":
							string bs = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref bs) == 0 &&
								Int32.TryParse(bs, out int conv_bs))
							{
								props.bs = conv_bs;
								i++;
							}
							break;
						case "d":
							props.isde = true;
							break;
					}
				}
			}
		}
	}
}
