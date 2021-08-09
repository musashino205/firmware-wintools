using System;
using System.Globalization;

namespace firmware_wintools.Tools
{
	static partial class ToolsArgMap
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、mkedimaximgの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">mkedimaximgの機能プロパティ</param>
		public static void
		Init_args_MkEdimaxImg(string[] args, int arg_idx, ref MkEdimaxImg.Properties subprops)
		{
			for (int i = arg_idx; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i].Replace("-", ""))
					{
						case "s":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.signature) == 0)
								i++;
							break;
						case "m":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.model) == 0)
								i++;
							break;
						case "f":
							string flash = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref flash) == 0 &&
								Program.StrToInt(flash, out int conv_flash,
										NumberStyles.None))
							{
								subprops.flash = conv_flash;
								i++;
							}
							break;
						case "S":
							string start = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref start) == 0 &&
								Program.StrToInt(start, out int conv_start,
										NumberStyles.None))
							{
								subprops.start = conv_start;
								i++;
							}
							break;
						case "b":
							subprops.isbe = true;
							break;
					}
				}
			}
		}
	}
}
