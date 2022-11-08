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
			for (int i = arg_idx; i < args.Length; i++)
			{
				if (!args[i].StartsWith("-"))
					continue;

				switch (args[i].Replace("-", ""))
				{
					case "d":	// directory
						if (ArgMap.Set_StrParamFromArgs(args, i, ref dir) == 0)
							i++;
						break;
				}
			}
		}
	}
}
