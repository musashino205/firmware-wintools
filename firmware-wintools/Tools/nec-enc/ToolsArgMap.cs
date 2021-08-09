namespace firmware_wintools.Tools
{
	static partial class ToolsArgMap
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、nec-encの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">nec-encの機能プロパティ</param>
		public static void
		Init_args_NecEnc(string[] args, int arg_idx, ref Tools.Nec_Enc.Properties subprops)
		{
			for (int i = arg_idx; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
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
}
