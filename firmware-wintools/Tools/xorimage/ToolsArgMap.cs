namespace firmware_wintools.Tools
{
	internal partial class XorImage
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
						if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.len) == 0)
							i++;
						break;
					case "O":	// offset
						string offset = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref offset) == 0 &&
							Utils.StrToInt(offset, out int conv_offset,
								System.Globalization.NumberStyles.None))
						{
							subprops.offset = conv_offset;
							i++;
						}
						break;
					case "p":
						if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.pattern) == 0)
							i++;
						break;
					case "r":
						subprops.rewrite = true;
						break;
					case "x":
						subprops.ishex = true;
						break;
				}
			}
		}
	}
}
