using System;
using System.IO;
using System.Text;
using firmware_wintools.Lang;

namespace firmware_wintools.Tools
{
	internal class Netgear_EncImg : Tool
	{
		public override string name => "netgear-encimg";
		public override string desc => "decrypt encrypted NETGEAR firmware images (marker: \"encrpted_img\")";
		public override string descFmt => "    {0}	: {1}";

		/* key and iv: scripts/netgear-encrypted-factory.py */
		private static byte[] KEY = new byte[]
		{
			0x68, 0x65, 0x39, 0x2d, 0x34, 0x2b, 0x4d, 0x21, 0x29, 0x64,
			0x36, 0x3d, 0x6d, 0x7e, 0x77, 0x65, 0x31, 0x2c, 0x71, 0x32,
			0x61, 0x33, 0x64, 0x31, 0x6e, 0x26, 0x32, 0x2a, 0x5a, 0x5e, 0x25, 0x38
		};

		private static byte[] IV = new byte[]
		{
			0x4a, 0x25, 0x31, 0x69, 0x51, 0x6c, 0x38, 0x24, 0x3d, 0x6c,
			0x6d, 0x2d, 0x3b, 0x38, 0x41, 0x45
		};

		private void PrintInfo(Header hdr, bool debug)
		{
			Console.WriteLine("Firmware  : {0} ({1}) {2}",
					Encoding.ASCII.GetString(hdr.model),
					Encoding.ASCII.GetString(hdr.region),
					Encoding.ASCII.GetString(hdr.version));
			Console.WriteLine("  created : {0}", Encoding.ASCII.GetString(hdr.created));
			Console.WriteLine("  length  : 0x{0:x08} ({0:N0} bytes)", hdr.datalen);
			if (debug)
				Console.WriteLine("  block   : 0x{0:x08} ({0:N0} bytes)", hdr.blklen);
		}

		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			Firmware fw = new Firmware();
			Header hdr;
			int ret;

			if (props.Help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
						FileAccess.Read, FileShare.Read))
				{
					long decryptlen;

					hdr = new Header();
					ret = hdr.LoadData(fw.inFs, Header.LENGTH);
					if (ret < 0)
					{
						Console.Error.WriteLine(Resource.Main_Error_Prefix
							+ "failed to load the header");
						return ret >= 0 ? 1 : ret;
					}
					hdr.DeserializeProps();
					if (!hdr.Validate())
					{
						Console.Error.WriteLine(Resource.Main_Error_Prefix
							+ "no valid header detected");
						return 1;
					}

					PrintInfo(hdr, props.Debug);
					decryptlen = hdr.datalen;
					if (decryptlen % 16 > 0)
						decryptlen += 16 - decryptlen % 16;

					using (fw.outFs = new FileStream(props.OutFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
					{
						byte[] buf = new byte[hdr.blklen];

						for (long offset = 0; offset < decryptlen; offset += hdr.blklen)
						{
							long declen = (decryptlen - offset < hdr.blklen) ?
										decryptlen - offset : hdr.blklen;
							/*
							 * 直接出力先のFileStreamをCryptoStreamにかませると
							 * 1回目の復号終了時にFileStreamもクローズしてしまう為、
							 * MemoryStreamで一旦復号データを受けてbyte配列から
							 * FileStreamに書き込む
							 */
							using (Stream ms = new MemoryStream(buf))
								Utils.AesData(256, KEY, IV, false,
									System.Security.Cryptography.CipherMode.CBC,
									System.Security.Cryptography.PaddingMode.None,
									fw.inFs, ms, declen);

							/* last block */
							if (declen < hdr.blklen)
								declen -= decryptlen - hdr.datalen;
							fw.outFs.Write(buf, 0, Convert.ToInt32(declen));
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

		private class Header : HeaderFooter
		{
			internal byte[] model = new byte[0x20];		/* 0x0 - 0x1f */
			internal byte[] region = new byte[0x20];	/* 0x20 - 0x3f */
			internal byte[] version = new byte[0x40];	/* 0x40 - 0x7f */
			internal byte[] created = new byte[0x40];   /* 0x80 - 0xbf */
			internal uint unknown1 = 0;					/* 0xc0 - 0xc3 */
			internal byte unknown2 = 0;					/* 0xc4 */
			internal byte hwids = 0;					/* 0xc5 */
			internal byte models = 0;					/* 0xc6 */
			internal byte[] rsvd1 = new byte[0xd];      /* 0xc7 - 0xd4 */
			internal byte[] hw_info = new byte[0xc8];	/* 0xd4 - 0x19b, ids + models */
			internal byte[] rsvd2 = new byte[0x64];		/* 0x19c - 0x1ff */
			internal byte[] mark = new byte[0xc];       /* 0x200 - 0x20b, "encrpted_img" */
			internal uint datalen = 0;					/* 0x20c - 0x20f */
			internal uint blklen = 0;                   /* 0x210 - 0x213 */

			[NonSerialized]
			internal const int LENGTH = 0x214;

			internal bool Validate()
				=> Encoding.ASCII.GetString(mark).Equals("encrpted_img");
		}
	}
}
