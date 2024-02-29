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
			string tmp;

			for (int i = arg_idx; i < args.Length; i++)
			{
				if (!args[i].StartsWith("-"))
					continue;

				switch (args[i].Replace("-", ""))
				{
					case "l":	// length
						if (Utils.GetStrParamOrKeep(args, i, ref len_s))
							i++;
						break;
					case "O":	// offset
						if (Utils.GetStrParam(args, i, out tmp) &&
						    Utils.StrToInt(tmp, out int conv_offset,
							    System.Globalization.NumberStyles.None))
						{
							offset = conv_offset;
							i++;
						}
						break;
					case "p":
						if (Utils.GetStrParamOrKeep(args, i, ref pattern))
							i++;
						break;
					case "P":
						if (Utils.GetStrParam(args, i, out binPattern))
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
