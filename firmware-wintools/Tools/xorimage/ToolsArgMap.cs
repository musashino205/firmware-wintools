namespace firmware_wintools.Tools
{
	partial class ToolsArgMap
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、xorimageの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">xorimageの機能プロパティ</param>
		public void Init_args_Xorimage(string[] args, ref Tools.XorImage.Properties props)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i].Replace("-", ""))
					{
						case "p":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref props.pattern) == 0)
								i++;
							break;
						case "x":
							props.ishex = true;
							break;
					}
				}
			}
		}
	}
}
