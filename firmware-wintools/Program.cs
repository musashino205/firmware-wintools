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
		}

		static void PrintHelp()
		{
			Console.WriteLine("Usage: firmware-wintools <func> [OPTIONS...]\n" +
				Environment.NewLine +
				"Functions:\n" +
				"  nec-enc: encode/decode the firmware for NEC Aterm series\n" +
				Environment.NewLine + Environment.NewLine +
				"For details in each functions, please run following:\n" +
				"  firmware-wintools <func> -h\n");
		}

		static int Main(string[] args)
		{
			int ret;
			Properties props = new Properties();

			if (args.Length < 1)
			{
				Console.Error.WriteLine("error: no command-line arguments");
				return 1;
			}

			ArgMap argMap = new ArgMap();
			argMap.Init_args(args, ref props);

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
					Console.Error.WriteLine("error: parameter error, exit");
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
				case "nec-enc":
					ret = Tools.nec_enc.Do_NecEnc(args, props);
					break;
				case "xorimage":
					ret = Tools.xorimage.Do_Xor(args, props);
					break;
				default:
					if (props.help)
					{
						PrintHelp();
						ret = 0;
					}
					else
					{
						Console.Error.WriteLine("error: mode is missing");
						ret = 1;
					}
					break;
			}

			if (ret != 0)
			{
				Console.Error.WriteLine("ERROR");
				if (props.debug)
					Thread.Sleep(4000);
				return ret;
			}

			Console.WriteLine("DONE");

			if (props.debug)
				Thread.Sleep(4000);
			return ret;
		}
	}
}
