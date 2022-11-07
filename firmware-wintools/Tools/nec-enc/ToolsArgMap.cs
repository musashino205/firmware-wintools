namespace firmware_wintools.Tools
{
	internal partial class Nec_Enc
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、nec-encの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">nec-encの機能プロパティ</param>
		private static void
		Init_args(string[] args, int arg_idx, ref Properties subprops)
		{
			for (int i = arg_idx; i < args.Length; i++)
			{
				if (!args[i].StartsWith("-"))
					continue;

				switch (args[i].Replace("-", ""))
				{
					case "k":
						if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.key) == 0)
							i++;
						break;
					case "H":
						subprops.half = true;
						break;
				}
			}
		}
	}
}
