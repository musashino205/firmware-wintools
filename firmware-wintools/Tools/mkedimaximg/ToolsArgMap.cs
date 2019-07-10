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
		public void Init_args_MkEdimaxImg(string[] args, ref Tools.MkEdimaxImg.Properties props)
		{
			CultureInfo provider = CultureInfo.CurrentCulture;
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i].Replace("-", ""))
					{
						case "s":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref props.signature) == 0)
								i++;
							break;
						case "m":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref props.model) == 0)
								i++;
							break;
						case "f":
							string flash = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref flash) == 0 &&
								Int32.TryParse((flash.StartsWith("0x") ? flash.Replace("0x", "") : flash),
								NumberStyles.HexNumber, provider, out int conv_flash))
							{
								props.flash = conv_flash;
								i++;
							}
							break;
						case "S":
							string start = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref start) == 0 &&
								Int32.TryParse(start.StartsWith("0x") ? start.Replace("0x", "") : start,
								NumberStyles.HexNumber, provider, out int conv_start))
							{
								props.start = conv_start;
								i++;
							}
							break;
						case "b":
							props.isbe = true;
							break;
					}
				}
			}
		}
	}
}
