﻿namespace firmware_wintools.Tools
{
	partial class ToolsArgMap
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、xorimageの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">xorimageの機能プロパティ</param>
		public void Init_args_BinCut(string[] args, int arg_idx, ref Tools.BinCut.Properties subprops)
		{
			for (int i = arg_idx; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i].Replace("-", ""))
					{
						case "l":	// length
							string length = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref length) == 0 &&
								Program.StrToLong(length, out long conv_length, 0) == 0)
							{
								subprops.len = conv_length;
								i++;

							}
							break;
						case "O":	// offset
							string offset = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref offset) == 0 &&
								Program.StrToInt(offset, out int conv_offset, 0) == 0)
							{
								subprops.offset = conv_offset;
								i++;
							}
							break;
						case "p":	// padding
							string pad = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref pad) == 0 &&
								Program.StrToInt(pad, out int conv_pad, 0) == 0)
							{
								subprops.pad = conv_pad;
								i++;
							}
							break;
						case "P":	// padding with blocksize
							string padBS = null;
							if (ArgMap.Set_StrParamFromArgs(args, i, ref padBS) == 0 &&
								Program.StrToInt(padBS, out int conv_padBS, 0) == 0)
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
}