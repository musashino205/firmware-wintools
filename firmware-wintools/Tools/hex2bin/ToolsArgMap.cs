namespace firmware_wintools.Tools
{
	internal partial class Hex2Bin
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
					case "H":   // skip first block
						skipFirstBlock = true;
						break;
					case "O":   // offset
						string offset_s = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref offset_s) == 0 &&
							Utils.StrToInt(offset_s, out int _offset,
										   System.Globalization.NumberStyles.None))
						{
							offset = _offset;
							i++;
						}
						break;
					case "t":	// table
						isTable = true;
						break;
					case "w":   // column width
						string width_s = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref width_s) == 0 &&
							Utils.StrToInt(width_s, out int _width,
										   System.Globalization.NumberStyles.None))
						{
							width = _width;
							i++;
						}
						break;
				}
			}
		}
	}
}
