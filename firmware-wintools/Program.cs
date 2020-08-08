using System;
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

		public static NumberStyles SetNumStyle(int numstyle, string valStr)
		{
			switch (numstyle)
			{
				case 0:		// auto
					if (valStr.StartsWith("0x"))
						goto case 16;
					else
						goto case 10;
				case 16:	// hex
					return NumberStyles.HexNumber;
				case 10:	// decimal
				default:	// octal（8進数）は.NETで非サポート、10進数として強制解釈
					return NumberStyles.Integer;
			}
		}

		public static int StrToInt(string val, out int cnv, int numstyle)
		{
			cnv = 0;
			CultureInfo provider = CultureInfo.CurrentCulture;

			if (val == null ||
				!Int32.TryParse(val.Replace("0x", ""), SetNumStyle(numstyle, val), provider, out cnv))
				return 1;

			return 0;
		}

		public static uint StrToUInt(string val, out uint cnv, int numstyle)
		{
			cnv = 0;
			CultureInfo provider = CultureInfo.CurrentCulture;

			if (val == null ||
				!UInt32.TryParse(val.Replace("0x", ""), SetNumStyle(numstyle, val), provider, out cnv))
				return 1;

			return 0;
		}

		public static long StrToLong(string val, out long cnv, int numstyle)
		{
			cnv = 0;
			CultureInfo provider = CultureInfo.CurrentCulture;

			if (val == null ||
				!Int64.TryParse(val.Replace("0x", ""), SetNumStyle(numstyle, val), provider, out cnv))
				return 1;

			return 0;
		}

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
			Console.WriteLine(Lang.Tools.AesRes.Main_FuncDesc_Fmt,		// aes
				"aes", Lang.Tools.AesRes.FuncDesc);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Main_FuncDesc_Fmt,	// buffalo-enc
				"buffalo-enc", Lang.Tools.BuffaloEncRes.FuncDesc);
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Main_FuncDesc_Fmt,	// mkedimaximg
				"mkedimaximg", Lang.Tools.MkEdimaxImgRes.FuncDesc);
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Main_FuncDesc_Fmt,	// mksenaofw
				"mksenaofw", Lang.Tools.MkSenaoFwRes.FuncDesc);
			Console.WriteLine(Lang.Tools.NecEncRes.Main_FuncDesc_Fmt,		// nec-enc
				"nec-enc", Lang.Tools.NecEncRes.FuncDesc);
			Console.WriteLine(Lang.Tools.XorImageRes.Main_FuncDesc_Fmt,		// xorimage
				"xorimage", Lang.Tools.XorImageRes.FuncDesc);
			Console.WriteLine(Environment.NewLine +
				Lang.Resource.Main_Help_DetailsMsg);
		}

		/// <summary>
		/// main関数として、各機能への分岐などを行います
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <returns>実行結果</returns>
		static int Main(string[] args)
		{
			int ret;
			Properties props = new Properties();
			string lc_all, lang, shell;

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

			if (args.Length == 0)		// 引数が0ならヘルプ表示して終了
			{
				PrintHelp();
				return 0;
			}

			ArgMap argMap = new ArgMap();
			argMap.Init_args(args, ref props);

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

			if (!props.help)
			{
				if (props.inFile == null || props.outFile == null)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Resource.Main_Error_NoInOutFile);
					if (props.debug)
						Thread.Sleep(4000);
					return 1;
				}

				Console.WriteLine(Lang.Resource.Main_Info + Environment.NewLine,
					Path.GetFileName(props.inFile), Directory.GetParent(props.inFile),
					Path.GetFileName(props.outFile), Directory.GetParent(props.outFile));
			}

			switch (args[0])
			{
				case "aes":
					Tools.Aes aes = new Tools.Aes();
					ret = aes.Do_Aes(args, props);
					break;
				case "buffalo-enc":
					Tools.Buffalo_Enc buffalo_enc = new Tools.Buffalo_Enc();
					ret = buffalo_enc.Do_BuffaloEnc(args, props);
					break;
				case "mkedimaximg":
					Tools.MkEdimaxImg mkedimaximg = new Tools.MkEdimaxImg();
					ret = mkedimaximg.Do_MkEdimaxImage(args, props);
					break;
				case "mksenaofw":
					Tools.MkSenaoFw mksenaofw = new Tools.MkSenaoFw();
					ret = mksenaofw.Do_MkSenaoFw(args, props);
					break;
				case "nec-enc":
					Tools.Nec_Enc nec_enc = new Tools.Nec_Enc();
					ret = nec_enc.Do_NecEnc(args, props);
					break;
				case "xorimage":
					Tools.XorImage xorimage = new Tools.XorImage();
					ret = xorimage.Do_XorImage(args, props);
					break;
				default:
					if (args[0].StartsWith("-") && props.help)
					{
						PrintHelp();
						ret = 0;
					}
					else
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix + Lang.Resource.Main_Error_NoInvalidMode);
						ret = 1;        // 指定されたモードが無効ならエラー吐いてret=1
					}
					break;
			}

			if (ret != 0)
				Console.Error.WriteLine("ERROR");
			else
				Console.Error.WriteLine("DONE");

			if (props.debug)
				Thread.Sleep(4000);
			return ret;
		}
	}
}
