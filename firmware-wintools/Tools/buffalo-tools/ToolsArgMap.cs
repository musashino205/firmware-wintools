using System;
using System.Globalization;

namespace firmware_wintools.Tools
{
	partial class ToolsArgMap
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、mkedimaximgの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">mkedimaximgの機能プロパティ</param>
		public void Init_args_BuffaloEnc(string[] args, ref Tools.Buffalo_Enc.Properties props)
		{
			CultureInfo provider = CultureInfo.CurrentCulture;
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i].Replace("-", ""))
					{
						case "d":
							props.isde = true;
							break;
						case "l":
							props.islong = true;
							break;
						case "k":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref props.crypt_key) == 0)
								i++;
							break;
						case "m":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref props.magic) == 0)
								i++;
							break;
						case "p":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref props.product) == 0)
								i++;
							break;
						case "v":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref props.version) == 0)
								i++;
							break;
						case "s":
							string seed = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref seed) == 0 &&
								byte.TryParse((seed.StartsWith("0x") ? seed.Replace("0x", "") : seed),
								NumberStyles.HexNumber, provider, out byte conv_seed))
							{
								props.seed = conv_seed;
								i++;
							}
							break;
						case "O":
							string offset = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref offset) == 0 &&
								Int32.TryParse(offset, out int conv_offset))
							{
								props.offset = conv_offset;
								i++;
							}
							break;
						case "S":
							string size = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref size) == 0 &&
								Int32.TryParse(size, out int conv_size))
							{
								props.size = conv_size;
								i++;
							}
							break;
					}
				}
			}
		}
	}
}
