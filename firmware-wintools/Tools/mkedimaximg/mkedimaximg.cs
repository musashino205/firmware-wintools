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
		private void PrintHelp(int arg_idx)
		{
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Help_Usage +
				Lang.Tools.MkEdimaxImgRes.FuncDesc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");	// 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption();
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				Lang.Tools.MkEdimaxImgRes.Help_Options_s +
				Lang.Tools.MkEdimaxImgRes.Help_Options_m +
				Lang.Tools.MkEdimaxImgRes.Help_Options_f +
				Lang.Tools.MkEdimaxImgRes.Help_Options_s2 +
				Lang.Tools.MkEdimaxImgRes.Help_Options_b);
		}

		/// <summary>
		/// mkedimaximgの実行情報を表示します
		/// </summary>
		/// <param name="props"></param>
		private void PrintInfo(Properties subprops)
		{
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info);
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info_Signature, subprops.signature);
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info_Model, subprops.model);
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info_FlashAddr, subprops.flash);
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info_StartAddr, subprops.start);
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info_BE, subprops.isbe.ToString());
		}

		/// <summary>
		/// <paramref name="buf"/> からデータ末尾に付加するchecksumの算出を行います
		/// </summary>
		/// <param name="buf">checksum算出対象データ</param>
		/// <param name="isbe">BEでの算出</param>
		/// <returns></returns>
		private ushort CalcCkSum(in byte[] buf, bool isbe)
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
		public int Do_MkEdimaxImage(string[] args, int arg_idx, Program.Properties props)
		{
			int read_len;
			ushort cksum;
			byte[] header_buf;
			byte[] buf;
			Properties subprops = new Properties();
			Header header = new Header()
			{
				sign = new byte[4],
				model = new byte[4],
				flash = 0,
				start = 0,
				size = 0
			};

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ToolsArgMap argMap = new ToolsArgMap();
			argMap.Init_args_MkEdimaxImg(args, ref subprops);

			if (subprops.signature == null || subprops.signature == "")
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_NoSignature);
				return 1;
			}
			else if (subprops.signature.Length != 4)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_InvalidSignatureLen);
				return 1;
			}

			if (subprops.model == null || subprops.model == "")
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_NoModel);
				return 1;
			}
			else if (subprops.model.Length != 4)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_InvalidModelLen);
				return 1;
			}

			if (subprops.flash == 0)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_NoInvalidFlash);
				return 1;
			}

			if (subprops.start == 0)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_NoInvalidStart);
				return 1;
			}

			if (!props.quiet)
				PrintInfo(subprops);

			header.sign = Encoding.ASCII.GetBytes(subprops.signature);
			header.flash = (subprops.isbe) ? IPAddress.HostToNetworkOrder(subprops.flash) : subprops.flash;
			header.start = (subprops.isbe) ? IPAddress.HostToNetworkOrder(subprops.start) : subprops.start;
			header.model = Encoding.ASCII.GetBytes(subprops.model);

			header_buf = new byte[Marshal.SizeOf(header)];

			FileStream inFs;
			FileStream outFs;
			FileMode outFMode =
				File.Exists(props.outFile) ? FileMode.Truncate : FileMode.Create;
			try
			{
				inFs = new FileStream(props.inFile, FileMode.Open, FileAccess.Read, FileShare.Read);
				outFs = new FileStream(props.outFile, outFMode, FileAccess.Write, FileShare.None);
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			if (inFs.Length > 0x7FFFFFFFu)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_LargeInFile);
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

			cksum = CalcCkSum(in buf, subprops.isbe);
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
