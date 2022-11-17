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
		/// メインプロパティ
		/// <para>プログラム全体で必要となるパラメータを管理</para>
		/// </summary>
		public struct Properties
		{
			/// <summary>
			/// 出力詳細化
			/// </summary>
			public bool debug;
			/// <summary>
			/// ヘルプ フラグ
			/// </summary>
			public bool help;
			/// <summery>
			/// コンソール出力低減
			/// </summery>
			public bool quiet;
			/// <summary>
			/// 入力ファイル パス
			/// </summary>
			public string inFile;
			/// <summary>
			/// 出力ファイル パス
			/// </summary>
			public string outFile;
			/// <summary>
			/// -* オプションの数（指定されたモードは含まない）
			/// </summary>
			public int paramcnt;
			/// <summary>
			/// 無効なパラメータの有無 ('-')
			/// </summary>
			public bool param_invalid;
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
			int ret, arg_idx = 1;
			string[] args;
			Properties props = new Properties();
			string lc_all, lang, shell;
			string mode;
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

			if (toolList.Exists(x => x.name == Path.GetFileName(args[0])))
				arg_idx = 0;            // symlinkからの呼び出しまたはバイナリ名が機能名の場合、機能名を取る為0スタート
			else
				if (args.Length == 1)   // 引数が1（firmware-wintoolsのパス）ならヘルプ表示して終了
				{
					PrintHelp();
					return 0;
				}


			ArgMap argMap = new ArgMap();
			argMap.Init_args(args, arg_idx, ref props);

			if (props.param_invalid)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Resource.Main_Error_InvalidParam);
				return 1;
			}

			if (props.paramcnt == 0)
				props.help = true;		// -* パラメータの個数が0なら指定されたモードの有無/有効性に関わらずhelpフラグを立てる

			if (props.debug)
			{
				Console.WriteLine("\n==== args ====");
				foreach (string arg in args)
				{
					Console.WriteLine(arg);
				}
				Console.WriteLine("=============\n");
			}


			mode = args[arg_idx];
			/*
			 * symlinkからの呼び出しまたはfirmware-wintools.exeを機能名に変更した場合、
			 * 実行ファイルパスからディレクトリパスと拡張子を除去して機能名を取得
			 */
			if (arg_idx == 0)
				mode = Path.GetFileNameWithoutExtension(mode);

			arg_idx += 1;	// インデックスをモード名の次（オプション）へ進める

			if (mode.StartsWith("-") && props.help) {
				PrintHelp();
				return 0;
			}

			tool = toolList.Find(x => x.name == mode);
			if (tool == null) {
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix
					+ Lang.Resource.Main_Error_NoInvalidMode);

				return -99;
			}

			if (!props.help)
			{
				if (props.inFile == null ||
				    (!tool.skipOFChk && props.outFile == null))
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Resource.Main_Error_NoInOutFile);
					if (props.debug)
						Thread.Sleep(4000);
					return 1;
				}

				if (!props.quiet && props.debug)
					Console.WriteLine(Lang.Resource.Main_Info + Environment.NewLine,
						Path.GetFileName(props.inFile), Directory.GetParent(props.inFile),
						tool.skipOFChk ? "-" : Path.GetFileName(props.outFile),
						tool.skipOFChk ? "-" : Directory.GetParent(props.outFile).ToString());
			}

			ret = tool.Do(args, arg_idx, props);

			return ret;
		}
	}
}
