using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace firmware_wintools.Tools
{
	internal class Nec_BsdFw : Tool
	{
		/* ツール情報　*/
		public override string name { get => "nec-bsdfw"; }
		public override string desc { get => "list or cut out BSD-based NEC firmware"; }
		public override string descFmt { get => "    {0}		: {1}"; }
		public override bool skipOFChk => true;


		private const uint BLKHDR_F_GZIP = 0x80000000;  /* BIT(31) */
		private const uint BLKHDR_F_LZMA = 0x20000000;	/* BIT(29) */
		private const uint BLKHDR_F_EXEC = 0x00020000;   /* BIT(17) */
		private const uint BLKHDR_F_COMP = BLKHDR_F_GZIP | BLKHDR_F_LZMA;

		private bool IsList = false;
		private bool Decompress = false;
		private int OutPos = 0;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 'l', PType = Param.PTYPE.BOOL, SetField = "IsList" },
			new Param() { PChar = 'd', PType = Param.PTYPE.BOOL, SetField = "Decompress" },
			new Param() { PChar = 'p', PType = Param.PTYPE.INT, SetField = "OutPos" }
		};

		/// <summary>
		/// nec-bsdfwの機能ヘルプを表示します
		/// </summary>
		public new void PrintHelp(int arg_idx)
		{
			Console.WriteLine("Usage: {0}nec-bsdfw [options...]\n" +
				desc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");   // 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
															// 共通オプション表示
			Program.PrintCommonOption(true);
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				Lang.CommonRes.Help_Options_o +
				"  -l\t\t\tshow list of data blocks instead of cutting out\n" +
				"  -d\t\t\tdecompress gzip/lzma compressed data\n" +
				"  -p <position>\t\tcut out the data at specified <position> (default: 0)");
		}

		private void PrintBlkHeader(in List<BlkHeader> headers, int index)
		{
			BlkHeader header = headers[index];

			Console.WriteLine("- Data {0}:", index);
			Console.WriteLine("  Flags                 : 0x{0:X08}",
					header.flags);
			Console.WriteLine("    GZIP Compressed       : {0}",
					(header.flags & BLKHDR_F_GZIP) != 0 ? "Yes" : "No");
			Console.WriteLine("    LZMA Compressed       : {0}",
					(header.flags & BLKHDR_F_LZMA) != 0 ? "Yes" : "No");
			Console.WriteLine("    Executable            : {0}",
					(header.flags & BLKHDR_F_EXEC) != 0 ? "Yes" : "No");
			Console.WriteLine("  Data Length (with hdr): 0x{0:X}",
					header.length);
			Console.WriteLine("  Header Length         : 0x18");
			Console.WriteLine("  Checksum              : 0x{0:X08}",
					header.cksum);
			Console.WriteLine("  Load Address          : 0x{0:X08}",
					header.loadaddr);
			Console.WriteLine("  Entry Point           : 0x{0:X08}",
					header.entryp);
			//Console.WriteLine("  ---");
			Console.WriteLine("  Header Offset         : 0x{0:X}",
					header.offset - BlkHeader.HDR_LEN);
			Console.WriteLine("  Data Offset           : 0x{0:X}",
					header.offset);
			Console.WriteLine();
		}

		/// <summary>
		/// nec-bsdfwメイン関数
		/// <para>NEC Aterm機用ファームウェア内のデータブロック一覧を表示
		/// または指定インデックスのデータを取り出し</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">Program内のメインプロパティ</param>
		/// <returns></returns>
		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			Firmware fw = new Firmware();
			List<BlkHeader> hdrs = new List<BlkHeader>();
			int ret;

			if (props.Help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return ret;

			fw.inFInfo = new FileInfo(props.InFile);

			if (!IsList &&
			    (props.OutFile == null || props.OutFile.Length == 0))
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							Lang.Resource.Main_Error_NoInOutFile);
				return 1;
			}

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					BlkHeader hdr;

					while (fw.inFs.Position < fw.inFs.Length) {
						hdr = new BlkHeader();
						ret = hdr.LoadData(fw.inFs, BlkHeader.HDR_LEN);
						if (ret != 0)
							break;
						hdr.DeserializeProps();
						if (!hdr.Validate())
						{
							if (hdrs.Count == 0)
							{
								fw.inFs.Seek(-0x8, SeekOrigin.Current);
								continue;
							}
							else
								break;
						}

						hdr.offset = fw.inFs.Position;
						hdrs.Add(hdr);
						if (IsList)
							PrintBlkHeader(in hdrs, hdrs.Count - 1);

						fw.inFs.Seek(hdr.length - BlkHeader.HDR_LEN,
							SeekOrigin.Current);
						if (hdr.length % 4 > 0)
							fw.inFs.Seek(4 - hdr.length % 4, SeekOrigin.Current);
					}
				}

				/* リスト表示の場合は終了 */
				if (IsList)
					return 0;

				if (OutPos > hdrs.Count - 1)
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"specified index doesn't exist");
					return 1;
				}

				if (Decompress &&
				    (hdrs[OutPos].flags & (BLKHDR_F_GZIP | BLKHDR_F_LZMA)) == 0)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Warning_Prefix +
						"specified data is not compressed with valid type (gzip/lzma), " +
						"output binary without decompression...");
					Decompress = false;
				}

				if (hdrs[OutPos].length - BlkHeader.HDR_LEN > 0)
					using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
								FileAccess.Read, FileShare.Read))
					using (fw.outFs = new FileStream(props.OutFile, FileMode.Create,
								FileAccess.Write, FileShare.None))
					{
						BlkHeader hdr = hdrs[OutPos];
						int read_len, data_len;
						uint flags = Decompress ?
								(hdr.flags & BLKHDR_F_COMP) : 0;

						fw.data = new byte[0x10000];
						data_len = hdr.length - BlkHeader.HDR_LEN;

						fw.inFs.Seek(hdrs[OutPos].offset, SeekOrigin.Begin);
						switch (flags)
						{
							case BLKHDR_F_GZIP:
								GZipStream gz
									= new GZipStream(fw.inFs, CompressionMode.Decompress);

								while ((read_len = gz.Read(fw.data, 0, fw.data.Length)) > 0)
									fw.outFs.Write(fw.data, 0, read_len);
								gz.Close();
								break;

							case BLKHDR_F_LZMA:
								SevenZip.Compression.LZMA.Decoder lzma
									= new SevenZip.Compression.LZMA.Decoder();
								byte[] buf = new byte[sizeof(int) * 2];
								long decode_len;

								fw.inFs.Read(buf, 0, 5);
								lzma.SetDecoderProperties(buf);

								fw.inFs.Read(buf, 0, sizeof(int) * 2);
								decode_len = BitConverter.ToInt64(buf, 0);

								lzma.Code(fw.inFs, fw.outFs, data_len, decode_len, null);
								break;

							default: /* uncompressed or unkown compression type */
								read_len = (data_len < fw.data.Length) ?
										data_len : fw.data.Length;
								while ((read_len = fw.inFs.Read(fw.data, 0, read_len)) > 0)
								{
									fw.outFs.Write(fw.data, 0, read_len);
									data_len -= read_len;
									read_len = (data_len < fw.data.Length) ?
											data_len : fw.data.Length;
								}

								if (data_len != 0)
								{
									Console.Error.WriteLine(
										Lang.Resource.Main_Error_Prefix +
										" failed to read block data");
									return 1;
								}
								break;
						}
					}
				else
					Console.Error.WriteLine(Lang.Resource.Main_Warning_Prefix +
							"the data block at specified position has no body data\n");

				PrintBlkHeader(hdrs, OutPos);
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			return 0;
		}

		private class BlkHeader : HeaderFooter
		{
			internal uint flags = 0;
			internal int length = 0;
			internal uint hdrlen = 0;
			internal uint cksum = 0;
			internal uint loadaddr = 0;
			internal uint entryp = 0;

			[NonSerialized]
			internal long offset = 0;
			[NonSerialized]
			internal const int HDR_LEN = 0x18;

			internal bool Validate()
				/*
				 * - フラグ上位16ビット反転が下位16ビットと異なる
				 * - ヘッダ長がBLKHDR_LEN (0x18)と異なる
				 */
				=> (~flags >> 16) == (flags & 0xffff) &&
				   hdrlen == HDR_LEN;
		}
	}
}
