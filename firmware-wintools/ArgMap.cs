namespace firmware_wintools
{
	class ArgMap
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) から文字列の引数を取得します
		/// <para>指定されたオプションの後ろに文字列が存在しない場合、取得をスキップします</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="index">文字列を取得するオプション ("-*") のインデックス</param>
		/// <param name="target">取得された文字列を格納するターゲット</param>
		/// <returns></returns>
		public static int Set_StrParamFromArgs(string[] args, int index, ref string target)
		{
			if (index + 1 < args.Length && !args[index + 1].StartsWith("-"))
			{
				target = args[index + 1];
				return 0;
			}
			else
			{
				return 1;
			}
		}

		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、メインプロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">取得したオプションを格納するターゲット</param>
		public void Init_args(string[] args, ref Program.Properties props)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i].Replace("-", ""))
					{
						case "i":
							if (Set_StrParamFromArgs(args, i, ref props.inFile) == 0)
								i++;
							break;
						case "o":
							if (Set_StrParamFromArgs(args, i, ref props.outFile) == 0)
								i++;
							break;
						case "h":
							props.help = true;
							break;
						case "D":
							props.debug = true;
							break;
						case "":    // ハイフンのみ ('-') 対策
							props.prop_invalid = true;
							break;
					}
					props.propcnt++;
				}
			}
		}
	}
}
