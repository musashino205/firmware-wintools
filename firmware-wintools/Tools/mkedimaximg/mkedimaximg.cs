using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace firmware_wintools.Tools
{
	class MkEdimaxImg
	{
		/// <summary>
		/// mkedimaximgの機能プロパティ
		/// </summary>
		public struct Properties
		{
			/// <summary>
			/// edimaxヘッダに付加するシグネチャ
			/// </summary>
			public string signature;
			/// <summary>
			/// edimaxヘッダに付加するモデル名
			/// </summary>
			public string model;
			/// <summary>
			/// edimaxヘッダに付加するフラッシュ アドレス
			/// </summary>
			public int flash;
			/// <summary>
			/// edimaxヘッダに付加するスタート アドレス
			/// </summary>
			public int start;
			/// <summary>
			/// edimaxヘッダの数値をBEで算出/書き込みを行うか否か
			/// </summary>
			public bool isbe;
		}

		/// <summary>
		/// edimaxヘッダ構造体
		/// </summary>
		public struct Header
		{
			public byte[] sign;
			public int start;
			public int flash;
			public byte[] model;
			public int size;
		}

		/// <summary>
		/// mkedimaximgの機能ヘルプを表示します
		/// </summary>
		private void PrintHelp()
		{
			Console.WriteLine("Usage: firmware-wintools xorimage [OPTIONS...]\n" +
				Environment.NewLine +
				"Options:\n" +
				"  -i <file>\t\tinput file\n" +
				"  -o <file>\t\toutput file\n" +
				"  -s <signature>\tuse <signature> for image header\n" +
				"  -m <model>\t\tuse <model> for image header\n" +
				"  -f <flash>\t\tuse the <flash> address for image header\n" +
				"  -S <start>\t\tuse the <start> address for image header\n" +
				"  -b\t\t\tuse \"big endian\" mode\n");
		}

		/// <summary>
		/// mkedimaximgの実行情報を表示します
		/// </summary>
		/// <param name="props"></param>
		private void PrintInfo(Properties props)
		{
			Console.WriteLine("===== mkedimaximg mode =====");
			Console.WriteLine(" signature:\t{0}", props.signature);
			Console.WriteLine(" model:\t\t{0}", props.model);
			Console.WriteLine(" flash addr:\t0x{0:X}", props.flash);
			Console.WriteLine(" start addr:\t0x{0:X}", props.start);
			Console.WriteLine(" BE mode:\t{0}\n", props.isbe.ToString());
		}

		/// <summary>
		/// <paramref name="buf"/> からデータ末尾に付加するchecksumの算出を行います
		/// </summary>
		/// <param name="buf">checksum算出対象データ</param>
		/// <param name="isbe">BEでの算出</param>
		/// <returns></returns>
		private ushort CalcCkSum(byte[] buf, bool isbe)
		{
			ushort cksum = 0;

			for (int i = 0; i < buf.Length / 2; i++)
			{
				if (isbe)
					cksum -= (ushort)IPAddress.HostToNetworkOrder(BitConverter.ToInt16(buf, i * 2));
				else
					cksum -= BitConverter.ToUInt16(buf, i);
			}

			return cksum;
		}

		/// <summary>
		/// mkedimaximgメイン関数
		/// <para>コマンドライン引数とメインプロパティから、edimaxヘッダと checksum
		/// の付加を行います</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">メインプロパティ</param>
		/// <returns></returns>
		public int Do_MkEdimaxImage(string[] args, Program.Properties props)
		{
			int read_len;
			ushort cksum;
			byte[] header_buf;
			byte[] buf;
			Properties subprops = new Properties();
			Header header = new Header()
			{
				sign = new byte[4],
				model = new byte[4]
			};

			header.flash = header.start = 0;
			header.size = 0;

			if (props.help)
			{
				PrintHelp();
				return 0;
			}

			ToolsArgMap argMap = new ToolsArgMap();
			argMap.Init_args_MkEdimaxImg(args, ref subprops);

			if (subprops.signature == null || subprops.signature == "")
			{
				Console.Error.WriteLine("error: no signature specified");
				return 1;
			}
			else if (subprops.signature.Length != 4)
			{
				Console.Error.WriteLine("error: signature must be 4 characters long");
				return 1;
			}

			if (subprops.model == null || subprops.model == "")
			{
				Console.Error.WriteLine("error: no model specified");
				return 1;
			}
			else if (subprops.model.Length != 4)
			{
				Console.Error.WriteLine("error: model must be 4 characters long");
				return 1;
			}

			if (subprops.flash == 0)
			{
				Console.Error.WriteLine("error: no or invalid flash address specified");
				return 1;
			}

			if (subprops.start == 0)
			{
				Console.Error.WriteLine("error: no or invalid start address specified");
				return 1;
			}

			PrintInfo(subprops);

			header.sign = Encoding.ASCII.GetBytes(subprops.signature);
			header.flash = (subprops.isbe) ? IPAddress.HostToNetworkOrder(subprops.flash) : subprops.flash;
			header.start = (subprops.isbe) ? IPAddress.HostToNetworkOrder(subprops.start) : subprops.start;
			header.model = Encoding.ASCII.GetBytes(subprops.model);

			header_buf = new byte[Marshal.SizeOf(header)];

			FileStream inFs;
			FileStream outFs;

			try
			{
				inFs = new FileStream(props.inFile, FileMode.Open, FileAccess.Read);
				outFs = new FileStream(props.outFile, FileMode.OpenOrCreate, FileAccess.Write);
			}
			catch (IOException i)
			{
				Console.Error.WriteLine(i.Message);
				return 1;
			}

			if (inFs.Length > 0xFFFFFFFFu)
			{
				Console.Error.WriteLine("error: input file is too large");
				return 1;
			}

			buf = new byte[inFs.Length];
			header.size = Convert.ToInt32(inFs.Length + sizeof(short));

			if (props.debug)
			{
				Console.WriteLine(" header size:\t{0} bytes (0x{0:X})", header_buf.Length);
				Console.WriteLine(" data size:\t{0} bytes (0x{0:X})", inFs.Length + sizeof(short));
				Console.WriteLine(" total size:\t{0} bytes (0x{0:X})", header_buf.Length + inFs.Length + sizeof(short));
			}

			if (subprops.isbe)
				header.size = IPAddress.HostToNetworkOrder(header.size);

			// struct -> byte[] 変換上手くいかない（文字列が化ける）ので個別に
			Array.Copy(header.sign, 0, header_buf, 0, sizeof(int));
			Array.Copy(BitConverter.GetBytes(header.start), 0, header_buf, sizeof(int), sizeof(int));
			Array.Copy(BitConverter.GetBytes(header.flash), 0, header_buf, sizeof(int) * 2, sizeof(int));
			Array.Copy(header.model, 0, header_buf, sizeof(int) * 3, sizeof(int));
			Array.Copy(BitConverter.GetBytes(header.size), 0, header_buf, sizeof(int) * 4, sizeof(int));

			outFs.Write(header_buf, 0, header_buf.Length);

			read_len = inFs.Read(buf, 0, buf.Length);
			outFs.Write(buf, 0, read_len);

			cksum = CalcCkSum(buf, subprops.isbe);
			if (props.debug)
				Console.WriteLine(" checksum:\t{0:X}\n", cksum);
			if (subprops.isbe)
				cksum = (ushort)IPAddress.HostToNetworkOrder((short)cksum);

			outFs.Write(BitConverter.GetBytes(cksum), 0, sizeof(ushort));

			inFs.Close();
			outFs.Close();
			return 0;
		}
	}
}
