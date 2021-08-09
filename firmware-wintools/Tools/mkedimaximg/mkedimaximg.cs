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
		/// mkedimaximgメイン関数
		/// <para>コマンドライン引数とメインプロパティから、edimaxヘッダと checksum
		/// の付加を行います</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">メインプロパティ</param>
		/// <returns></returns>
		public int Do_MkEdimaxImage(string[] args, int arg_idx, Program.Properties props)
		{
			Properties subprops = new Properties();
			EdimaxFirmware fw = new EdimaxFirmware();

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ToolsArgMap argMap = new ToolsArgMap();
			argMap.Init_args_MkEdimaxImg(args, arg_idx, ref subprops);

			fw.inFInfo = new FileInfo(props.inFile);
			fw.outFile = props.outFile;
			fw.outFMode = FileMode.Create;

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

			if (fw.inFInfo.Length > int.MaxValue)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_LargeInFile);
				return 1;
			}

			if (!props.quiet)
				PrintInfo(subprops);

			fw.dataLen = Convert.ToInt32(fw.inFInfo.Length + sizeof(short));

			/* ヘッダ設定 */
			fw.header.sign = Encoding.ASCII.GetBytes(subprops.signature);
			fw.header.start = subprops.isbe ?
						IPAddress.HostToNetworkOrder(subprops.start) :
						subprops.start;
			fw.header.flash = subprops.isbe ?
						IPAddress.HostToNetworkOrder(subprops.flash) :
						subprops.flash;
			fw.header.model = Encoding.ASCII.GetBytes(subprops.model);
			fw.header.size = subprops.isbe ?
						IPAddress.HostToNetworkOrder(fw.dataLen) :
						fw.dataLen;

			fw.totalLen = fw.header.size + fw.inFInfo.Length;

			fw.header.totalLen = sizeof(byte) * 4	// sign
					+ sizeof(int) * 2	// start, flash
					+ sizeof(byte) * 4	// model
					+ sizeof(int);		// size

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					fw.data = new byte[fw.inFInfo.Length];
					Firmware.FileToBytes(in fw.inFs, ref fw.data, fw.inFInfo.Length);
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			fw.footer.cksum = fw.CalcCksum(subprops.isbe);
			if (props.debug)
			{
				Console.WriteLine(" header size:\t{0} bytes (0x{0:X})", fw.header.totalLen);
				Console.WriteLine(" data size:\t{0} bytes (0x{0:X})", fw.inFInfo.Length + sizeof(short));
				Console.WriteLine(" total size:\t{0} bytes (0x{0:X})", fw.header.totalLen + fw.inFInfo.Length + sizeof(short));
				Console.WriteLine(" checksum:\t{0:X}\n", fw.footer.cksum);
			}

			/* ヘッダシリアル化 */
			fw.headerData = new byte[fw.header.totalLen];
			if (fw.header.SerializeProps(ref fw.headerData, 0) != fw.header.totalLen)
				return 1;

			/* BEモード時フッタcksumをBE変換 */
			if (subprops.isbe)
				fw.footer.cksum = (ushort)IPAddress.HostToNetworkOrder((short)fw.footer.cksum);
			/* フッタシリアル化 */
			fw.footerData = new byte[sizeof(ushort)];
			fw.footer.SerializeProps(ref fw.footerData, 0);

			return fw.OpenAndWriteToFile(false);
		}
	}
}
