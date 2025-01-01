using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace firmware_wintools
{
	class Program
	{
		/// <summary>
		/// 基本オプション用クラス
		/// <para>Tools.Toolを利用し基本オプションのInitArgsを呼ぶ</para>
		/// </summary>
		public class Properties : Tools.Tool
		{
			internal bool Debug = false;
			internal bool Help = false;
			internal bool Quiet = false;
			internal string InFile = null;
			internal string OutFile = null;

			internal override List<Tools.Param> ParamList => new List<Tools.Param>()
			{
				new Tools.Param() { PChar = 'i', PType = Tools.Param.PTYPE.STR, SetField = "InFile" },
				new Tools.Param() { PChar = 'o', PType = Tools.Param.PTYPE.STR, SetField = "OutFile" },
				new Tools.Param() { PChar = 'h', PType = Tools.Param.PTYPE.BOOL, SetField = "Help" },
				new Tools.Param() { PChar = 'D', PType = Tools.Param.PTYPE.BOOL, SetField = "Debug" },
				new Tools.Param() { PChar = 'Q', PType = Tools.Param.PTYPE.BOOL, SetField = "Quiet" }
			};

			internal override int Do(string[] args, int arg_idx, Properties props)
				=> InitArgs(args, arg_idx);
		}

		/// <summary>
		/// 各ツールクラスのリスト
		/// </summary>
		private static readonly List<Tools.Tool> toolList = new List<Tools.Tool>() {
			new Tools.Aes(),
			new Tools.BinCut(),
			new Tools.Buffalo_Enc(),
			new Tools.MkEdimaxImg(),
			new Tools.MkSenaoFw(),
			new Tools.Nec_Enc(),
			new Tools.XorImage(),

			/* misc tools */
			new Tools.RtkWeb(),
			new Tools.Hex2Bin(),
			new Tools.Nec_BsdFw(),
			new Tools.Nec_BsdFFS(),
			new Tools.Nec_WAEnc(),
			new Tools.Netgear_EncImg(),
			new Tools.Silex_Enc(),
			new Tools.Woff_Head(),
			new Tools.NosImg_Enc(),
		};

		/// <summary>
		/// 本体ヘルプを表示
		/// </summary>
		static void PrintHelp()
		{
			Assembly asm = Assembly.GetExecutingAssembly();
			Console.WriteLine(Lang.Resource.Main_Help_NameVer,
				((AssemblyProductAttribute)asm.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0]).Product,
				asm.GetName().Version);
			Console.WriteLine(Lang.Resource.Main_Help_Usage +
				Environment.NewLine +
				Lang.Resource.Main_Help_Functions);

			foreach (Tools.Tool tool in toolList)
				Console.WriteLine(tool.descFmt, tool.name, tool.desc);

			Console.WriteLine(Environment.NewLine +
				Lang.Resource.Main_Help_DetailsMsg);

			PrintCommonOption();
		}

		/// <summary>
		/// 共通ヘルプを表示
		/// </summary>
		public static void PrintCommonOption(bool skipOFChk)
		{
			Console.WriteLine(Lang.CommonRes.Help_CommonOpts +
				Lang.CommonRes.Help_Options_i +
				(skipOFChk ? "" : Lang.CommonRes.Help_Options_o) +
				Lang.CommonRes.Help_Options_Q);
		}

		public static void PrintCommonOption()
		{
			PrintCommonOption(false);
		}


		/// <summary>
		/// main関数として、各機能への分岐などを行います
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <returns>実行結果</returns>
		static int Main()
		{
			int ret, arg_idx = 0;
			string[] args;
			Properties props = new Properties();
			string lc_all, lang, shell;
			Tools.Tool tool;

			lc_all = Environment.GetEnvironmentVariable("LC_ALL");
			lang = Environment.GetEnvironmentVariable("LANG");
			shell = Path.GetFileName(Environment.GetEnvironmentVariable("SHELL"));

			lang = lc_all != null ? lc_all : (lang != null ? lang : "");
			shell = shell != null ? shell : "";

			if (lang == "" && shell == "") {	// PowerShell or CMD
				CultureInfo curCul = CultureInfo.CurrentCulture;
				/* How to get current encoding in console? */
				//Console.OutputEncoding = 
			} else {							// MinGW or others
				Regex langReg = new Regex(".*\\.");
				lang = langReg.Replace(lang, "");

				switch (lang.ToLower()) {
					case "eucjp":	// EUC-JP
						Console.OutputEncoding = Encoding.GetEncoding(lang.ToLower());
						break;
					case "sjis":	// Shift-JIS
						Console.OutputEncoding = Encoding.GetEncoding(lang.ToLower());
						break;
					case "utf-8":
					case "c":
					default:
						Console.OutputEncoding = Encoding.UTF8;
						break;
				}
			}

			args = Environment.GetCommandLineArgs();

			/* 実行ファイル名からモード取得試行 */
			tool = toolList.Find(x => x.name == Path.GetFileNameWithoutExtension(args[arg_idx]));
			arg_idx++;
			if (tool == null) /* 実行ファイル名が機能名ではない */
			{
				if (args.Length == 1) /* - 引数無し */
				{
					PrintHelp();
					return 0;
				}

				/* 最初の引数からモード取得試行 */
				tool = toolList.Find(x => x.name == args[arg_idx]);
				if (tool != null)
					arg_idx++;
			}

			/* 引数が無しまたは機能名のみ */
			if (args.Length == arg_idx)
				props.Help = true;

			ret = props.Do(args, arg_idx, null);
			if (ret < 0) /* エラー */
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Resource.Main_Error_InvalidParam);
				return ret;
			}

			/* 最初の引数からのモード取得試行に失敗した場合 */
			if (tool == null)
			{
				/* 最初の引数がモードではなく、ヘルプが指定されている */
				if (args[1].StartsWith("-") && props.Help)
				{
					PrintHelp();
					return 0;
				}

				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix
					+ Lang.Resource.Main_Error_NoInvalidMode);

				return -22;
			}

			if (!props.Help)
			{
				/* 入出力ファイル指定有無チェック */
				if (props.InFile == null ||
				    (!tool.skipOFChk && props.OutFile == null))
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Resource.Main_Error_NoInOutFile);
					return 1;
				}

				/* 入力ファイル存在チェック */
				if (!File.Exists(props.InFile))
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Resource.Main_Error_MissInputFile);
					return 1;
				}

				/* ファイル情報表示（デバッグ） */
				if (!props.Quiet && props.Debug)
					Console.WriteLine(Lang.Resource.Main_Info + Environment.NewLine,
						Path.GetFileName(props.InFile), Directory.GetParent(props.InFile),
						tool.skipOFChk ? "-" : Path.GetFileName(props.OutFile),
						tool.skipOFChk ? "-" : Directory.GetParent(props.OutFile).ToString());
			}

			ret = tool.Do(args, arg_idx, props);

			return ret;
		}
	}
}
