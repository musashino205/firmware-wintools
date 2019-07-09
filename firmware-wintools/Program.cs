using System;
using System.IO;
using System.Threading;

namespace firmware_wintools
{
	class Program
	{
		public struct Properties
		{
			public bool debug;
			public bool help;
			public string inFile;
			public string outFile;
			public int propcnt;
		}

		static void PrintHelp()
		{
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
					Console.Error.WriteLine("error: mode is not specified or invalid mode is specified");
					ret = 1;		// 指定されたモードが無効ならエラー吐いてret=1
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
