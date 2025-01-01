using System;
using System.Collections.Generic;
using System.IO;

namespace firmware_wintools.Tools
{
	internal class NosImg_Enc : Tool
	{
		/* ツール情報 */
		public override string name => "nosimg-enc";
		public override string desc => "encode/decode nos.img binary for XikeStor switches";
		public override string descFmt => "    {0}\t\t: {1}";

		private const int ENCODE_BLKLEN = 0x100;
		private const int ENCODE_BLOCKS = 2;
		private readonly uint[] patterns = new uint[] {
			0xeeddcc21,  0x5355eecc,  0xdd55807e,  0x00000000,
			0xcdbddfae,  0xbb9b8901,  0x70e5ccdd,  0xf6fc8364,
			0xecddcef1,  0xe354fed0,  0xbdabdde1,  0xe4b4d583,
			0xedfed0cd,  0xb655cca3,  0xedd5c67e,  0xddcc2153,
			0xec4ddc00,  0x5355cdc3,  0x2201807e,  0xefbc7566,
			0xa6c0cc2f,  0xfed0eecc,  0xdd550101,  0x0101c564,
			0x9945ab32,  0x55807eef,  0x55807eef,  0xbc756689,
			0xe31d83dd,  0xfe558eab,  0x7d55807e,  0xff01ac66,
			0x0ec992d9,  0x73e50101,  0xbde510ce,  0x0101bae8,
			0x3edd81a1,  0x53330101,  0x9ac510aa,  0x01ce8ae1,
			0xb1fb0080,  0x53770000,  0x70dc0001,  0x0000cbb1,
			0xa0300000,  0x55a60000,  0xcabd0101,  0x0000c9b2,
			0x81900100,  0x5a210001,  0x79bc0100,  0x78007bb3,
			0xd4970100,  0x5355a9fc,  0xdda501be,  0xafc175c5,
			0x8ed77700,  0x55d00dac,  0x0155807e,  0xefbc7ee6,
			0xf16c5200,  0x331698cc,  0x01010101,  0x00007988,
		};

		private bool Decode = false;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 'd', PType = Param.PTYPE.BOOL, SetField = "Decode" },
		};

		private new void PrintHelp(int arg_idx)
		{
			Console.WriteLine("Usage: {0}nosimg-enc [options...]\n" +
				desc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools "); // 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption(false);
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				"  -d\t\t\tdecode the specified image instead of encoding\n");
		}

		private byte GetBytePattern(int index)
		{
			uint tmp;

			/* 配列からブロック取り出し */
			tmp = patterns[index / sizeof(uint) % patterns.Length];
			/* 4byteから1byte取り出し */
			tmp >>= (sizeof(uint) - index % sizeof(uint) - 1) * 8;

			return Convert.ToByte(tmp & 0xff);
		}

		private int EncodeBlock(ref byte[] data, int p_off)
		{
			for (int i = 0; i < data.Length; i++, p_off++)
				data[i] -= (byte)(GetBytePattern(p_off) * (Decode ? -1 : 1));

			return p_off;
		}

		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{

			Firmware fw = new Firmware();
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
				using (fw.outFs = new FileStream(props.OutFile, FileMode.Create,
						FileAccess.Write, FileShare.None))
				{
					int p_off = 0;
					byte[] buf = new byte[ENCODE_BLKLEN];

					for (int i = 0; i < ENCODE_BLOCKS; i++)
					{
						int readlen = fw.inFs.Read(buf, 0, buf.Length);

						p_off = EncodeBlock(ref buf, p_off);
						fw.outFs.Write(buf, 0, readlen);
						if (readlen < buf.Length)
							break;
					}

					fw.inFs.CopyTo(fw.outFs);
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				ret = 1;
			}

			return ret;
		}
	}
}
