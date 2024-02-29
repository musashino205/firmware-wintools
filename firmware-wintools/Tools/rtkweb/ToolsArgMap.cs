namespace firmware_wintools.Tools
{
	internal partial class RtkWeb
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
					case "d":	// directory
						if (Utils.GetStrParamOrKeep(args, i, ref dir))
							i++;
						break;
					case "H":
						if (Utils.GetStrParam(args, i, out tmp) &&
						    Utils.StrToInt(tmp, out int conv_finfo_len,
							    System.Globalization.NumberStyles.None))
						{
							finfo_len = conv_finfo_len;
							i++;
						}
						break;
				}
			}
		}
	}
}
