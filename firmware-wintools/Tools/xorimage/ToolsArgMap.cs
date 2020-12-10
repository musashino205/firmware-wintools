namespace firmware_wintools.Tools
{
	partial class ToolsArgMap
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、xorimageの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">xorimageの機能プロパティ</param>
		public void Init_args_Xorimage(string[] args, ref Tools.XorImage.Properties subprops)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i].Replace("-", ""))
					{
						case "l":	// length
							if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.len) == 0)
								i++;
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
						case "p":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.pattern) == 0)
								i++;
							break;
						case "x":
							subprops.ishex = true;
							break;
					}
				}
			}
		}
	}
}
