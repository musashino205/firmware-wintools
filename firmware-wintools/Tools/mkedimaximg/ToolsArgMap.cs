using System.Globalization;

namespace firmware_wintools.Tools
{
	internal partial class MkEdimaxImg
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、mkedimaximgの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">mkedimaximgの機能プロパティ</param>
		public static void
		Init_args(string[] args, int arg_idx, ref Properties subprops)
		{
			string tmp;

			for (int i = arg_idx; i < args.Length; i++)
			{
				if (!args[i].StartsWith("-"))
					continue;

				switch (args[i].Replace("-", ""))
				{
					case "s":
						if (Utils.GetStrParam(args, i, out subprops.signature))
							i++;
						break;
					case "m":
						if (Utils.GetStrParam(args, i, out subprops.model))
							i++;
						break;
					case "f":
						if (Utils.GetStrParam(args, i, out tmp) &&
						    Utils.StrToInt(tmp, out int conv_flash,
							    NumberStyles.None))
						{
							subprops.flash = conv_flash;
							i++;
						}
						break;
					case "S":
						if (Utils.GetStrParam(args, i, out tmp) &&
						    Utils.StrToInt(tmp, out int conv_start,
							    NumberStyles.None))
						{
							subprops.start = conv_start;
							i++;
						}
						break;
					case "b":
						subprops.isbe = true;
						break;
				}
			}
		}
	}
}
