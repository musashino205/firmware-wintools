using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using firmware_wintools.Lang;

namespace firmware_wintools.Tools
{
	internal class Silex_Enc : Tool
	{
		public override string name => "silex-enc";
		public override string desc => "decode firmware of Silex devices";
		public override string descFmt => "    {0}\t\t: {1}";

		private byte[] patterns = new byte[] {
			0xc3, 0xf3, 0xbc, 0x8a, 0xcc, 0xe5, 0xd7, 0x9f
		};

		static StreamWriter sw = new StreamWriter("log.txt");

		private void PrintHelp()
		{

		}

		private void PrintBlock(BlockHeader header, ushort cksum)
		{
			string line = string.Format("{0:x04} {1:x04} {2:x08} {3:x04} (cksum: {4:x04})",
					header.unknown1, header.blkidx, header.decoffs, header.datalen, cksum);
			Console.WriteLine(line);

			//sw.WriteLine(line);
			//sw.Flush();
		}

		private int DecodeBlock(ref byte[] data, out int dataoffs, out int datalen)
		{
			BlockHeader header = new BlockHeader();
			uint cksum = 0;
			ushort embsum;

			header.Data = new byte[BlockHeader.BLKHDR_LEN];
			Buffer.BlockCopy(data, 0, header.Data, 0, header.Data.Length);
			header.DeserializeProps();

			if (header.datalen != BlockHeader.BLKDATA_LEN)
			{ }

			embsum = BitConverter.ToUInt16(data, BlockHeader.BLKHDR_LEN + header.datalen);
			embsum = (ushort)Utils.BE16toHost(embsum);

			if (header.datalen != BlockHeader.BLKDATA_LEN)
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

		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			Firmware fw = new Firmware();
			int ret;

			if (props.Help)
			{
				PrintHelp();
				return 0;
			}

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
						FileAccess.Read, FileShare.Read))
				{
					FWHeader header = new FWHeader();
					int readlen;
					byte[] buf = new byte[BlockHeader.BLKHDR_LEN
								+ BlockHeader.BLKDATA_LEN
								+ sizeof(ushort)];

					/* firmware header */
					if ((readlen = fw.inFs.Read(buf, 0, FWHeader.DEFAULT_LEN))
							!= FWHeader.DEFAULT_LEN)
					{
						Console.Error.WriteLine(Resource.Main_Error_Prefix +
							"failed to read header");
						return 1;
					}

					ret = header.Parse(buf);
					if (ret !=  0)
					{
						Console.Error.WriteLine(Resource.Main_Error_Prefix +
							"failed to parse header");
						return 1;
					}

					fw.inFs.Seek(header.hdrlen, SeekOrigin.Begin);

					using (fw.outFs = new FileStream(props.OutFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
					{
						List<DataEntry> entries = new List<DataEntry>();
						int dataoffs, datalen;

						while ((readlen = fw.inFs.Read(buf, 0, buf.Length)) > 0)
						{
							/* ヘッダ長 + チェックサム長未満 */
							if (readlen < BlockHeader.BLKHDR_LEN + sizeof(ushort))
							{
								Console.Error.WriteLine(Resource.Main_Error_Prefix +
									"failed to read data block");
								return 1;
							}

							ret = DecodeBlock(ref buf, out dataoffs, out datalen);
							if (ret < 0)
							{
								Console.Error.WriteLine(Resource.Main_Error_Prefix +
									"failed to decode block");
								return ret;
							}
							else if (ret == BlockHeader.ENDBLK_IDX)
								break;

							int proclen = BlockHeader.BLKHDR_LEN + sizeof(ushort);

							if (datalen > 0)
							{
								if (dataoffs > fw.outFs.Position)
									fw.outFs.Seek(dataoffs, SeekOrigin.Begin);
								fw.outFs.Write(buf, 0, datalen);
								proclen += datalen;
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

				/* verlen */
				verlen = Utils.BE16toHost(BitConverter.ToInt16(data, offset));
				offset += sizeof(short);
				version = new byte[verlen];
				Buffer.BlockCopy(data, offset, version, 0, verlen);
				offset += verlen;
				Encoding.ASCII.GetString(version);

				/* pad */
				for (int i = 0; i < pad.Length; i++, offset += sizeof(uint))
					pad[i] = BitConverter.ToUInt32(data, offset);

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
			internal int decoffs = 0;
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
			internal int offset = 0;
			internal int length = 0;
		}
	}
}
