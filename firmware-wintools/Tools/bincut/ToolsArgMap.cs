using System.Globalization;

namespace firmware_wintools.Tools
{
	internal partial class BinCut
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、xorimageの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">xorimageの機能プロパティ</param>
		public static void
		Init_args(string[] args, int arg_idx, ref Properties subprops)
		{
			for (int i = arg_idx; i < args.Length; i++)
			{
				if (!args[i].StartsWith("-"))
					continue;

				switch (args[i].Replace("-", ""))
				{
					case "l":	// length
						string length = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref length) == 0 &&
							Utils.StrToLong(length, out long conv_length,
									NumberStyles.None))
						{
							subprops.len = conv_length;
							i++;
						}
						break;
					case "O":	// offset
						string offset = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref offset) == 0 &&
							Utils.StrToInt(offset, out int conv_offset,
									NumberStyles.None))
						{
							subprops.offset = conv_offset;
							i++;
						}
						break;
					case "p":	// padding
						string pad = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref pad) == 0 &&
							Utils.StrToInt(pad, out int conv_pad,
									NumberStyles.None))
						{
							subprops.pad = conv_pad;
							i++;
						}
						break;
					case "P":	// padding with blocksize
						string padBS = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref padBS) == 0 &&
							Utils.StrToInt(padBS, out int conv_padBS,
									NumberStyles.None))
						{
							subprops.padBS = conv_padBS;
							i++;
						}
						break;
				}
			}
		}
	}
}
