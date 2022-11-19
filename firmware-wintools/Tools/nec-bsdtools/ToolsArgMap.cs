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
						if (ArgMap.Set_StrParamFromArgs(args, i, ref outDir) == 0)
							i++;
						break;
					case "H":
						skipHardLink = false;
						break;
					case "L":
						listToText = true;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref outTxt) == 0)
							i++;
							goto case "l";
					case "l":   // List Mode
						isListMode = true;
						break;
				}
			}
		}
	}

	internal partial class Nec_BsdFw
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
					case "l":	// List Mode
						isListMode = true;
						break;
					case "o":	// output
						if (ArgMap.Set_StrParamFromArgs(args, i, ref output) == 0)
							i++;
						break;
					case "p":   // output position
						string outPos_s = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref outPos_s) == 0 &&
							Utils.StrToInt(outPos_s, out int _outPos,
								   System.Globalization.NumberStyles.None))
						{
							outPos = _outPos;
							i++;
						}
						break;
				}
			}
		}
	}
}
