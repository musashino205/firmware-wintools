using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace firmware_wintools.Tools
{
	internal partial class Nec_BsdFw : Tool
	{
		/* ツール情報　*/
		public override string name { get => "nec-bsdfw"; }
		public override string desc { get => "list or cut out BSD-based NEC firmware"; }
		public override string descFmt { get => "    {0}		: {1}"; }
		public override bool skipOFChk => true;


		internal bool isListMode = false;
		internal string output = null;
		internal int outPos = 0;

		private readonly int BLKHDR_LEN = 0x18;
		private readonly uint BLKHDR_F_GZIP = 0x80000000;	/* BIT(31) */
		private readonly uint BLKHDR_F_EXEC = 0x00020000;   /* BIT(17) */

		/// <summary>
		/// rtkwebの機能ヘルプを表示します
		/// </summary>
		public void PrintHelp(int arg_idx)
		{
			Console.WriteLine("Usage: {0}rtkweb [options...]\n" +
				desc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");   // 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
															// 共通オプション表示
			Program.PrintCommonOption(true);
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				Lang.CommonRes.Help_Options_o +
				"  -l\t\t\tshow list of data blocks instead of cutting out\n" +
				"  -p <position>\t\tcut out the data at specified <position> (default: 0)");
		}

		private int CheckBlkHeader(in byte[] hdrAry, out BlkHeader header)
		{
			int i = 0;
			uint val;
			FieldInfo[] fields = typeof(BlkHeader).GetFields(BindingFlags.Instance |
								BindingFlags.NonPublic);

			header = new BlkHeader();

			foreach (var field in fields)
			{
				if (field.IsNotSerialized)
					continue;
				val = (uint)Utils.BE32toHost(BitConverter.ToInt32(hdrAry, i));
				if (field.FieldType == typeof(int))
					field.SetValue(header, (int)val);
				else
					field.SetValue(header, val);
				i += sizeof(uint);
			}

			/* フラグ集 */
			val = header.flags;
			if ((~val >> 16) != (val & 0xffff))
				return 1;

			/* ヘッダ長 */
			val = header.hdrlen;
			if (val != 0x18)
				return 1;

			return 0;
		}

		private void PrintBlkHeader(in BlkHeader header, int index)
		{
			Console.WriteLine("- Data {0}:", index);
			Console.WriteLine("  Flags                 : 0x{0:X08}",
					header.flags);
			Console.WriteLine("    GZIP Compressed       : {0}",
					((header.flags & BLKHDR_F_GZIP) != 0).ToString());
			Console.WriteLine("    Executable            : {0}",
					((header.flags & BLKHDR_F_EXEC) != 0).ToString());
			Console.WriteLine("  Data Length (with hdr): 0x{0:X}",
					header.length);
			Console.WriteLine("  Header Length         : 0x18");
			Console.WriteLine("  Checksum              : 0x{0:X08}",
					header.cksum);
			Console.WriteLine("  Load Address          : 0x{0:X08}",
					header.loadaddr);
			Console.WriteLine("  Entry Point           : 0x{0:X08}",
					header.entryp);
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

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			Init_args(args, arg_idx);

			fw.inFInfo = new FileInfo(props.inFile);

			if (!isListMode && output == null)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							Lang.Resource.Main_Error_NoInOutFile);
				return 1;
			}

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					byte[] hdrbuf = new byte[BLKHDR_LEN];
					BlkHeader hdr = null;
					int read_len, ret = 0;

					while (fw.inFs.Position < fw.inFs.Length) {
						read_len = fw.inFs.Read(hdrbuf, 0, BLKHDR_LEN);
						if (read_len < BLKHDR_LEN)
							break;

						ret = CheckBlkHeader(in hdrbuf, out hdr);
						if (ret != 0)
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
						if (isListMode)
							PrintBlkHeader(in hdr, hdrs.Count - 1);

						fw.inFs.Seek(hdr.length - BLKHDR_LEN, SeekOrigin.Current);
						if (hdr.length % 4 > 0)
							fw.inFs.Seek(4 - hdr.length % 4, SeekOrigin.Current);
					}
				}

				/* リスト表示の場合は終了 */
				if (isListMode)
					return 0;

				if (outPos > hdrs.Count - 1)
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"specified index doesn't exist");
					return 1;
				}

				if (hdrs[outPos].length - BLKHDR_LEN > 0)
					using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
								FileAccess.Read, FileShare.Read))
					using (fw.outFs = new FileStream(props.outFile, FileMode.Create,
								FileAccess.Write, FileShare.None))
					{
						int read_len, data_len;
						BlkHeader hdr = hdrs[outPos];

						data_len = hdr.length - BLKHDR_LEN;
						fw.data = new byte[data_len];

						fw.inFs.Seek(hdrs[outPos].offset, SeekOrigin.Begin);
						read_len = fw.inFs.Read(fw.data, 0, data_len);
						if (read_len != data_len)
						{
							Console.Error.WriteLine(
									Lang.Resource.Main_Error_Prefix +
									" failed to read block data");
							return 1;
						}

						fw.outFs.Write(fw.data, 0, data_len);
					}
				else
					Console.Error.WriteLine(Lang.Resource.Main_Warning_Prefix +
							"the data block at specified position has no body data\n");

				PrintBlkHeader(hdrs[outPos], outPos);
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			return 0;
		}

		private class BlkHeader
		{
			internal uint flags = 0;
			internal int length = 0;
			internal uint hdrlen = 0;
			internal uint cksum = 0;
			internal uint loadaddr = 0;
			internal uint entryp = 0;

			[NonSerialized]
			internal long offset = 0;
		}
	}
}
