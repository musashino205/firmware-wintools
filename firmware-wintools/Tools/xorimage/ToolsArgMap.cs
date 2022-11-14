namespace firmware_wintools.Tools
{
	internal partial class XorImage
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、xorimageの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">xorimageの機能プロパティ</param>
		public void
		Init_args(string[] args, int arg_idx)
		{
			for (int i = arg_idx; i < args.Length; i++)
			{
				if (!args[i].StartsWith("-"))
					continue;

				switch (args[i].Replace("-", ""))
				{
					case "l":	// length
						if (ArgMap.Set_StrParamFromArgs(args, i, ref len_s) == 0)
							i++;
						break;
					case "O":	// offset
						string offset_s = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref offset_s) == 0 &&
							Utils.StrToInt(offset_s, out int conv_offset,
								System.Globalization.NumberStyles.None))
						{
							offset = conv_offset;
							i++;
						}
						break;
					case "p":
						if (ArgMap.Set_StrParamFromArgs(args, i, ref pattern) == 0)
							i++;
						break;
					case "P":
						if (ArgMap.Set_StrParamFromArgs(args, i, ref binPattern) == 0)
							i++;
						break;
					case "r":
						rewrite = true;
						break;
					case "x":
						ishex = true;
						break;
				}
			}
		}
	}
}
