using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace firmware_wintools.Tools
{
	internal class Woff_Head : Tool
	{
		public override string name => "woff-head";
		public override string desc => "dump font header information of WOFF/WOFF2";
		public override string descFmt => "    {0}\t\t: {1}";
		public override bool skipOFChk => true;

		private bool ModOnly = false;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 'm', PType = Param.PTYPE.BOOL, SetField = "ModOnly" },
		};

		private byte[]
		ReadAndDecompressTableV1(FileStream stream, Woff.V1.TableEntry entry)
		{
			byte[] buf = new byte[entry.cmplen];

			stream.Seek(entry.offset, SeekOrigin.Begin);
			if (stream.Read(buf, 0, buf.Length) != entry.cmplen)
				return null;
			/* uncompressed data is stored */
			if (entry.cmplen == entry.ucmplen)
				return buf;

			return Utils.ZlibDecompress(buf, Convert.ToInt32(entry.ucmplen));
		}

		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			Firmware fw = new Firmware();
			Woff.V1.Header header;
			List<Woff.V1.TableEntry> entries;
			Woff.HeaderTable table;
			int ret;

			if (props.Help)
			{
				return 0;
			}

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return 1;

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
						FileAccess.Read, FileShare.Read))
				{
					Woff.V1.TableEntry entry;
					byte[] buf = new byte[4];
					int readlen;

					if ((readlen = fw.inFs.Read(buf, 0, buf.Length))
						!= buf.Length)
					{
						Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"failed to read magic number");
						return 1;
					}

					/* magic numberからWOFF/WOFF2判定 */
					switch (Encoding.ASCII.GetString(buf, 0, readlen))
					{
						case Woff.V1.Header.MAGIC:
							header = new Woff.V1.Header();
							break;
						case Woff.V2.Header.MAGIC:
							//TBD
							//header = new Woff.V2.Header();
							//break;
						default:
							Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
								"no valid WOFF magic number detected");
							return 1;
					}

					header.Data = new byte[Woff.V1.Header.HDRLEN];
					fw.inFs.Seek(0, SeekOrigin.Begin);
					/*
					 * - FileStream.Read() の読み取りデータ長がヘッダ長と異なる
					 * - header.DeserializeProps() での処理済みデータ長がヘッダ長と異なる
					 */
					if ((readlen = fw.inFs.Read(header.Data, 0, header.Data.Length))
						!= Woff.V1.Header.HDRLEN ||
					    (ret = header.DeserializeProps()) != Woff.V1.Header.HDRLEN)
					{
						Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"failed to read WOFF header");
						return 1;
					}

					if (props.Debug)
						Console.Error.WriteLine("Table Entries:");
					entries = new List<Woff.V1.TableEntry>();
					for (int i = 0; i < header.tables; i++)
					{
						entry = new Woff.V1.TableEntry();
						entry.Data = new byte[Woff.V1.TableEntry.ENTRYLEN];

						if ((readlen = fw.inFs.Read(entry.Data, 0, entry.Data.Length))
							!= entry.Data.Length ||
						    (ret = entry.DeserializeProps()) != entry.Data.Length)
						{
							Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
								"failed to read table entries");
							return 1;
						}

						entries.Add(entry);
						if (props.Debug)
							Console.Error.WriteLine(entry.ToEntryString());
					}

					entry = entries.Find(x => Encoding.ASCII.GetString(x.tag).Equals("head"));
					if (entry == null)
					{
						Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"no \"head\" entry found");
						return 1;
					}

					table = new Woff.HeaderTable()
					{ Data = ReadAndDecompressTableV1(fw.inFs, entry) };
					if (table.Data == null)
					{
						Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"failed to decompress table");
						return 1;
					}
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			ret = table.DeserializeProps();
			if (ret != Woff.HeaderTable.TABLELEN)
				return 1;

			Console.WriteLine(table.InfoToString(ModOnly));

			return 0;
		}

		internal class Woff
		{
			internal class V1
			{
				internal class Header : HeaderFooter
				{
					internal uint signature = 0;	/* 0x0 */
					internal uint flavor = 0;
					internal uint length = 0;
					internal ushort tables = 0;
					internal ushort reserved = 0;
					internal uint ucmptotlen = 0;	/* 0x10 */
					internal ushort major = 0;
					internal ushort minor = 0;
					internal uint metaoffs = 0;
					internal uint metalen = 0;
					internal uint ucmpmetalen = 0;	/* 0x20 */
					internal uint privoffs = 0;
					internal uint privlen = 0;

					[NonSerialized]
					internal const string MAGIC = "wOFF";
					[NonSerialized]
					internal const int HDRLEN = 0x2c;
				}

				internal class TableEntry : HeaderFooter
				{
					internal byte[] tag = new byte[4];
					internal uint offset = 0;
					internal uint cmplen = 0;
					internal uint ucmplen = 0;
					internal uint ucmpcsum = 0;

					[NonSerialized]
					internal const int ENTRYLEN = 0x14;

					internal string ToEntryString()
						=> string.Format(
							"  \"{0}\": 0x{1:x08} @ 0x{2:x08} (decompressed: {3:N0} bytes, checksum: 0x{4:x08})",
							Encoding.ASCII.GetString(tag), cmplen, offset, ucmplen, ucmpcsum);
				}
			}

			internal class V2
			{
				internal class Header : HeaderFooter
				{
					internal uint signature = 0;	/* 0x0 */
					internal uint flavor = 0;
					internal uint length = 0;
					internal ushort tables = 0;
					internal ushort reserved = 0;
					internal uint ucmptotlen = 0;	/* 0x10 */
					internal uint cmpdatlen = 0;
					internal ushort major = 0;
					internal ushort minor = 0;
					internal uint metaoffs = 0;
					internal uint metalen = 0;		/* 0x20 */
					internal uint ucmpmetalen = 0;
					internal uint privoffs = 0;
					internal uint privlen = 0;

					[NonSerialized]
					internal const string MAGIC = "wOF2";
					[NonSerialized]
					internal const int HDRLEN = 0x30;
				}
			}

			internal class HeaderTable : HeaderFooter
			{
				internal ushort major = 0;		/* 0x0 */
				internal ushort minor = 0;
				internal ushort revision1 = 0;
				internal ushort revision2 = 0;
				internal uint checksum = 0;		/* 0x8 */
				internal uint magic = 0;
				internal ushort flags = 0;		/* 0x10 */
				internal ushort units = 0;
				internal long created = 0;		/* 0x14 */
				internal long modified = 0;		/* 0x1c */
				internal short xmin = 0;		/* 0x24 */
				internal short ymin = 0;
				internal short xmax = 0;
				internal short ymax = 0;
				internal ushort style = 0;		/* 0x28 */
				internal ushort lwstppem = 0;
				internal short dirhint = 0;		/* 0x30 */
				internal short locfmt = 0;
				internal short glydatfmt = 0;

				[NonSerialized]
				internal const int TABLELEN = 0x36; /* 54 bytes */

				internal string
				InfoToString(bool modonly)
				{
					DateTime dt = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
					string lines;

					if (!modonly)
						lines = string.Format(
								"magic   : {0:X08}\n" +
								"version : {1}.{2}\n" +
								"revision: {3}.{4}\n" +
								"checksum: {5:X08}\n" +
								"style   : {6:x}\n" +
								"created : {7}" +
								"modified: {8}",
								magic,
								major, minor,
								revision1, revision2,
								checksum,
								style,
								dt.AddSeconds(created).ToString("yyyy/MM/dd HH:mm:ss\n"),
								dt.AddSeconds(modified).ToString("yyyy/MM/dd HH:mm:ss\n"));
					else
						lines = dt.AddSeconds(modified).ToString("yyyy/MM/dd HH:mm:ss");

					return lines;
				}
			}
		}
	}
}
