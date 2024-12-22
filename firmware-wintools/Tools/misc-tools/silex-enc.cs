using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace firmware_wintools.Tools
{
	internal class Silex_Enc : Tool
	{
		public override string name => "silex-enc";
		public override string desc => "decode firmware of Silex devices";
		public override string descFmt => "    {0}\t\t: {1}";

		private bool RealOffset = false;
		private bool Debug = false;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 'r', PType = Param.PTYPE.BOOL, SetField = "RealOffset" },
		};

		private byte[] patterns = new byte[] {
			0xc3, 0xf3, 0xbc, 0x8a, 0xcc, 0xe5, 0xd7, 0x9f
		};

		private new void PrintHelp(int arg_idx)
		{
			Console.WriteLine("Usage: {0}silex-enc [options...]\n" +
				desc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools "); // 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption(true);
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				Lang.CommonRes.Help_Options_o +
				"  -r\t\t\tdecode data entries to real offsets instead of the addresses starting from 0");
		}

		private void PrintBlock(BlockHeader header, ushort cksum)
		{
			string line = string.Format("{0:x04} {1:x04} {2:x08} {3:x04} (cksum: {4:x04})",
					header.unknown1, header.blkidx, header.decoffs, header.datalen, cksum);
			Console.WriteLine(line);
		}

		private int DecodeBlock(ref byte[] data, out uint dataoffs, out int datalen)
		{
			BlockHeader header = new BlockHeader();
			uint cksum = 0;
			ushort embsum;

			header.Data = new byte[BlockHeader.BLKHDR_LEN];
			Buffer.BlockCopy(data, 0, header.Data, 0, header.Data.Length);
			header.DeserializeProps();

			embsum = BitConverter.ToUInt16(data, BlockHeader.BLKHDR_LEN + header.datalen);
			embsum = (ushort)Utils.BE16toHost(embsum);

			if (Debug && header.datalen != BlockHeader.BLKDATA_LEN)
				PrintBlock(header, embsum);

			/* 始端ブロックまたは終端ブロック */
			dataoffs = header.decoffs;
			datalen = header.datalen;
			if ((header.blkidx == 0 && datalen == 0) ||
			    (header.blkidx == 0xffff && datalen == 0))
				return header.blkidx;

			/* チェックサム算出とデータのデコード */
			for (int i = 0; i < header.datalen; i++)
			{
				cksum += data[BlockHeader.BLKHDR_LEN + i];
				data[i] = (byte)(data[BlockHeader.BLKHDR_LEN + i]
						^ patterns[(header.blkidx - 1) % 8]);
			}

			return ((cksum & 0xffff) != embsum) ? -1 : header.blkidx;
		}

		private void PrintFWInfo(FWHeader header, List<DataEntry> entries)
		{
			int i = 1;

			Console.Error.WriteLine("{0} v{1}\n",
					Encoding.ASCII.GetString(header.model),
					Encoding.ASCII.GetString(header.version));

			Console.WriteLine("#  offset\tlength");
			foreach (DataEntry entry in entries)
			{
				Console.WriteLine("{0}  0x{1:X08}\t0x{2:X08} ({2:N0} bytes)",
						i++, entry.offset, entry.length);
			}
		}

		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			List<DataEntry> entries = new List<DataEntry>();
			Firmware fw = new Firmware();
			FWHeader header;
			int ret;

			if (props.Help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return ret;

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
						FileAccess.Read, FileShare.Read))
				{
					header = new FWHeader();
					int readlen;
					byte[] buf = new byte[BlockHeader.BLKHDR_LEN
								+ BlockHeader.BLKDATA_LEN
								+ sizeof(ushort)];

					/* firmware header */
					if ((readlen = fw.inFs.Read(buf, 0, FWHeader.DEFAULT_LEN))
							!= FWHeader.DEFAULT_LEN)
					{
						Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"failed to read header");
						return 1;
					}

					ret = header.Parse(buf);
					if (ret !=  0)
					{
						Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"failed to parse header");
						return 1;
					}

					fw.inFs.Seek(header.hdrlen, SeekOrigin.Begin);

					using (fw.outFs = new FileStream(props.OutFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
					{
						DataEntry entry = null;
						uint baseoffs = 0, dataoffs;
						int datalen;
						int i = 0;

						while ((readlen = fw.inFs.Read(buf, 0, buf.Length)) > 0)
						{
							/* ヘッダ長 + チェックサム長未満 */
							if (readlen < BlockHeader.BLKHDR_LEN + sizeof(ushort))
							{
								Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
									"failed to read data block");
								return 1;
							}

							ret = DecodeBlock(ref buf, out dataoffs, out datalen);
							if (ret < 0)
							{
								Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
									"failed to decode block");
								return ret;
							}
							else if (ret == BlockHeader.ENDBLK_IDX)
							{
								if (entry != null)
									entries.Add(entry);
								break;
							}

							int proclen = BlockHeader.BLKHDR_LEN + sizeof(ushort);

							if (datalen > 0)
							{
								/*
								 * SKY-AP-301AN等で 0xF81E0000 など巨大なアドレスを
								 * 指していることがあり、出力先バイナリが大きくなりすぎるので
								 * "-r" オプション未指定時は最初のデータのオフセットを
								 * ベースとして扱う
								 */
								if (!RealOffset && ret == 1)
									baseoffs = dataoffs;

								/*
								 * ブロックのオフセットが出力先データの現在位置から飛んでいる
								 * -> 次のデータ開始
								 */
								if ((dataoffs - baseoffs) > fw.outFs.Position)
								{
									fw.outFs.Seek(dataoffs - baseoffs, SeekOrigin.Begin);
									i = 0;
									if (entry != null)
										entries.Add(entry);
								}
								fw.outFs.Write(buf, 0, datalen);
								proclen += datalen;

								if (i == 0)
								{
									entry = new DataEntry();
									entry.offset = dataoffs - baseoffs;
								}

								entry.length += datalen;
								i++;
							}

							fw.inFs.Seek(-(readlen - proclen), SeekOrigin.Current);
						}
					}
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			PrintFWInfo(header, entries);

			return 0;
		}

		internal class FWHeader
		{
			internal byte[] magic = new byte[0x4];
			internal short hdrlen = 0;
			internal short modellen = 0;
			internal byte[] model = null;
			internal short verlen = 0;
			internal byte[] version = null;
			internal uint[] pad = new uint[8];
			internal int datalen = 0;
			internal int blocks = 0;

			[NonSerialized]
			internal const int DEFAULT_LEN = 0x80;
			[NonSerialized]
			internal const string MAGICSTR = ".VUP";

			internal int Parse(in byte[] data)
			{
				int offset = 0;

				/* magic */
				Buffer.BlockCopy(data, offset, magic, 0, magic.Length);
				if (!Encoding.ASCII.GetString(magic).Equals(MAGICSTR))
					return 1;
				offset += magic.Length;

				/* hdrlen */
				hdrlen = Utils.BE16toHost(BitConverter.ToInt16(data, offset));
				offset += sizeof(short);

				/* model (length, string) */
				modellen = Utils.BE16toHost(BitConverter.ToInt16(data, offset));
				offset += sizeof(short);
				model = new byte[modellen];
				Buffer.BlockCopy(data, offset, model, 0, modellen);
				offset += modellen;
				Encoding.ASCII.GetString(model);

				/* version (length, string) */
				verlen = Utils.BE16toHost(BitConverter.ToInt16(data, offset));
				offset += sizeof(short);
				version = new byte[verlen];
				Buffer.BlockCopy(data, offset, version, 0, verlen);
				offset += verlen;
				Encoding.ASCII.GetString(version);

				/* pad */
				for (int i = 0; i < pad.Length; i++, offset += sizeof(uint))
					pad[i] = (uint)Utils.BE32toHost(BitConverter.ToUInt32(data, offset));

				/* datalen */
				datalen = Utils.BE32toHost(BitConverter.ToInt32(data, offset));
				offset += sizeof(uint);

				/* blocks */
				blocks = Utils.BE32toHost(BitConverter.ToInt32(data, offset));
				offset += sizeof(uint);

				return (offset != hdrlen) ? 1 : 0;
			}
		}

		internal class BlockHeader : HeaderFooter
		{
			internal ushort unknown1 = 0;
			internal ushort blkidx = 0;
			internal uint decoffs = 0;
			internal ushort datalen = BLKDATA_LEN;

			[NonSerialized]
			internal const int BLKHDR_LEN = 0xa;
			[NonSerialized]
			internal const ushort BLKDATA_LEN = 0x1e0;
			[NonSerialized]
			internal const ushort ENDBLK_IDX = 0xffff;
		}

		internal class DataEntry
		{
			internal uint offset = 0;
			internal int length = 0;
		}
	}
}
