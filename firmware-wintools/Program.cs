using System;
using System.IO;
using System.Reflection;
using System.Threading;

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
			public int propcnt;
			/// <summary>
			/// 無効なパラメータの有無 ('-')
			/// </summary>
			public bool prop_invalid;
		}

		/// <summary>
		/// 本体ヘルプを表示
		/// </summary>
		static void PrintHelp()
		{
			Assembly asm = Assembly.GetExecutingAssembly();
			Console.WriteLine("{0}  Version: {1}\n",
				((AssemblyProductAttribute)asm.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0]).Product,
				asm.GetName().Version);
			Console.WriteLine("Usage: firmware-wintools <func> [OPTIONS...]\n" +
				Environment.NewLine +
				"Functions:\n" +
				"    mkedimaximg:\tadd Edimax header and checksum\n" +
				"    nec-enc:\t\tencode/decode firmware for NEC Aterm series\n" +
				"    xorimage:\t\tencode/decode firmware by xor with a pattern\n" +
				Environment.NewLine + Environment.NewLine +
				"For details in each functions, please run following:\n" +
				"  firmware-wintools <func> -h\n");
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

			if (args.Length == 0)		// 引数が0ならヘルプ表示して終了
			{
				PrintHelp();
				return 0;
			}

			ArgMap argMap = new ArgMap();
			argMap.Init_args(args, ref props);

			if (props.prop_invalid)
			{
				Console.Error.WriteLine("error: invalid parameter is specified");
				return 1;
			}

			if (props.propcnt == 0)
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
					Console.Error.WriteLine("error: input or output file is not specified");
					if (props.debug)
						Thread.Sleep(4000);
					return 1;
				}

				Console.WriteLine("********** Info **********\n" +
					" input file:\t{0}\n\t\t({1})\n\n" +
					" output file:\t{2}\n\t\t({3})\n\n",
					Path.GetFileName(props.inFile), Directory.GetParent(props.inFile),
					Path.GetFileName(props.outFile), Directory.GetParent(props.outFile));
			}

			switch (args[0])
			{
				case "mkedimaximg":
					Tools.MkEdimaxImg mkedimaximg = new Tools.MkEdimaxImg();
					ret = mkedimaximg.Do_MkEdimaxImage(args, props);
					break;
				case "nec-enc":
					Tools.Nec_Enc nec_enc = new Tools.Nec_Enc();
					ret = nec_enc.Do_NecEnc(args, props);
					break;
				case "xorimage":
					Tools.XorImage xorimage = new Tools.XorImage();
					ret = xorimage.Do_Xor(args, props);
					break;
				default:
					if (args[0].StartsWith("-") && props.help)
					{
						PrintHelp();
						ret = 0;
					}
					else
					{
						Console.Error.WriteLine("error: mode is not specified or invalid mode is specified");
						ret = 1;        // 指定されたモードが無効ならエラー吐いてret=1
					}
					break;
			}

			if (ret != 0)
				Console.Error.WriteLine("ERROR");
			else
				Console.WriteLine("DONE");

			if (props.debug)
				Thread.Sleep(4000);
			return ret;
		}
	}
}
