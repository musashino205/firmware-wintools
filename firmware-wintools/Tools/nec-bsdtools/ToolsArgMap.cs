namespace firmware_wintools.Tools
{
	internal partial class Nec_BsdFFS
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
					case "d":
						if (Utils.GetStrParamOrKeep(args, i, ref outDir))
							i++;
						break;
					case "f":
						if (Utils.GetStrParamOrKeep(args, i, ref outFsBin))
							i++;
						break;
					case "H":
						skipHardLink = false;
						break;
					case "L":
						listToText = true;
						if (Utils.GetStrParamOrKeep(args, i, ref outTxt))
							i++;
							goto case "l";
					case "l":   // List Mode
						isListMode = true;
						break;
				}
			}
		}
	}
}
