using System;
using System.IO;
using System.Net;
using System.Text;

namespace firmware_wintools.Tools
{
	class Buffalo_Enc
	{
		const string DEFAULT_KEY = "Buffalo";
		const string DEFAULT_MAGIC = "start";

		public struct Properties
		{
			public string crypt_key;
			public string magic;
			public bool islong;
			public byte seed;
			public string product;
			public string version;
			public bool isde;
			public int offset;
			public int size;
			public bool force;
		}

		private void PrintHelp(int arg_idx)
		{
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Help_Usage +
				Lang.Tools.BuffaloEncRes.FuncDesc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");	// 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption();
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				Lang.Tools.BuffaloEncRes.Help_Options_d +
				Lang.Tools.BuffaloEncRes.Help_Options_l +
				Lang.Tools.BuffaloEncRes.Help_Options_k +
				Lang.Tools.BuffaloEncRes.Help_Options_m +
				Lang.Tools.BuffaloEncRes.Help_Options_p +
				Lang.Tools.BuffaloEncRes.Help_Options_v +
				Lang.Tools.BuffaloEncRes.Help_Options_o2 +
				Lang.Tools.BuffaloEncRes.help_Options_S +
				Lang.Tools.BuffaloEncRes.Help_Options_F,
				DEFAULT_KEY, DEFAULT_MAGIC);
		}

		private void PrintInfo(Properties subprops, long datalen, uint cksum, bool isdbg)
		{
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info, subprops.isde ?
				Lang.Tools.BuffaloEncRes.Info_Decrypt : Lang.Tools.BuffaloEncRes.Info_Encrypt);
			if (isdbg)
			{
				Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Longstate, subprops.islong);
				Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Key, subprops.crypt_key);
			}
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Magic, subprops.magic);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Seed, subprops.seed);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Product, subprops.product);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Version, subprops.version);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_DataLen, datalen);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Cksum, cksum);
		}

		private int CheckParams(Properties subprops)
		{
			if (subprops.crypt_key == null || subprops.crypt_key.Length == 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_NoKey);
				return 1;
			}
			else if (subprops.crypt_key.Length > BufBcrypt.BCRYPT_MAX_KEYLEN)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_LongKey,
					subprops.crypt_key);
				return 1;
			}

			if (subprops.magic.Length != BufEncHeader.ENC_MAGIC_LEN - 1)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_InvalidMagicLen,
					BufEncHeader.ENC_MAGIC_LEN - 1);
				return 1;
			}

			if (!subprops.isde)
			{
				if (subprops.product == null)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_NoProduct);
					return 1;
				}
				else if (subprops.product.Length > BufEncHeader.ENC_PRODUCT_LEN - 1)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_LongProduct,
						subprops.product);
					return 1;
				}

				if (subprops.version == null)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_NoVersion);
					return 1;
				}
				else if (subprops.version.Length > BufEncHeader.ENC_VERSION_LEN - 1)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_LongVersion,
						subprops.version);
					return 1;
				}
			}

			return 0;
		}

		private int Encrypt(ref BufEncFirmware fw, Properties subprops, Program.Properties props)
		{
			int ret;
			byte[] key;

			/* magic, product, versionは末尾 0x0 ('\0') で終端 */
			BufEncHeader header = new BufEncHeader()
			{
				magic = Encoding.ASCII.GetBytes(subprops.magic + "\0"),
				seed = subprops.seed,
				prod_len = subprops.product.Length + 1,
				product = Encoding.ASCII.GetBytes(subprops.product + "\0"),
				ver_len = subprops.version.Length + 1,
				version = Encoding.ASCII.GetBytes(subprops.version + "\0"),
				data_len = subprops.size > 0 ?
						(uint)subprops.size : (uint)fw.inFs.Length
			};

			BufEncFooter footer = new BufEncFooter();

			/* キーは 0x00 ('\0') で終端 */
			key = Encoding.ASCII.GetBytes(subprops.crypt_key + "\0");

			/* ヘッダ/データ/フッタの各部長さ */
			header.totalLen = header.GetHeaderLen();	// ヘッダ
			fw.dataLen = header.data_len;			// データ
			footer.totalLen = sizeof(uint);			// フッタ1
			/* フッタ4byteパディング分 */
			footer.totalLen +=				// フッタ2
				4 - (footer.totalLen + header.totalLen + fw.dataLen) % 4;

			fw.data = new byte[fw.dataLen];
			Firmware.FileToBytes(in fw.inFs, ref fw.data, fw.dataLen);

			footer.cksum = fw.GetCksum();

			if (!props.quiet)
				PrintInfo(subprops, fw.dataLen, footer.cksum, props.debug);

			/* ヘッダ暗号化（+ product長, ver長, データ長BE変換） */
			ret = header.EncryptHeader(key, subprops.islong);
			if (ret > 0)
				return ret;
			/* ヘッダシリアル化 */
			fw.header = new byte[header.totalLen];
			if (header.SerializeProps(ref fw.header, 0) != header.totalLen)
				return 1;

			/* フッタcksumをBE変換 */
			footer.cksum = (uint)IPAddress.HostToNetworkOrder((int)footer.cksum);
			/* フッタシリアル化 */
			fw.footer = new byte[footer.totalLen];
			footer.SerializeProps(ref fw.footer, 0);

			/* データ暗号化 */
			if (fw.EncryptData(header.dataSeed, key, subprops.islong) < 0)
				return 1;

			try
			{
				fw.outFs = new FileStream(fw.outFile, FileMode.Create, FileAccess.Write, FileShare.None);
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}
			ret = fw.WriteToFile(false);

			/* サイズ指定ある場合残りのデータを出力ファイルにコピー */
			if (subprops.size > 0)
				fw.inFs.CopyTo(fw.outFs);

			fw.outFs.Close();

			return ret;
		}

		private int Decrypt(ref BufEncFirmware fw, Properties subprops, Program.Properties props)
		{
			int ret;
			uint cksum;
			byte[] key;

			BufEncHeader header = new BufEncHeader();
			BufEncFooter footer = new BufEncFooter();

			if (!fw.inFs.CanSeek || fw.inFs.Length < subprops.offset)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_LargeOffset,
					subprops.offset);
				return 1;
			}

			fw.inFs.Seek(subprops.offset, SeekOrigin.Begin);

			key = Encoding.ASCII.GetBytes(subprops.crypt_key);
			/* ヘッダ読み込み */
			ret = header.LoadHeader(fw.inFs, in key, subprops.islong);
			if (ret > 0)
				return ret;

			/* データ読み込み */
			ret = fw.LoadData(header.data_len, header.dataSeed, in key, subprops.islong);
			if (ret > 0)
				return ret;

			/* フッタ読み込み */
			ret = footer.LoadFooter(fw.inFs);
			if (ret > 0)
				return ret;

			fw.dataLen = header.data_len;
			cksum = fw.GetCksum();
			/* 計算cksumと埋め込みcksum不一致 & 非forceならエラー */
			if (!subprops.force && cksum != footer.cksum)
				return 1;
			subprops.magic = Encoding.ASCII.GetString(header.magic).TrimEnd('\0');
			subprops.seed = header.seed;
			subprops.product = Encoding.ASCII.GetString(header.product).TrimEnd('\0');
			subprops.version = Encoding.ASCII.GetString(header.version).TrimEnd('\0');
			subprops.size = Convert.ToInt32(header.data_len);

			if (!props.quiet)
				PrintInfo(subprops, header.data_len, footer.cksum, props.debug);

			fw.WriteToFile(true);

			return 0;
		}

		public int Do_BuffaloEnc(string[] args, int arg_idx, Program.Properties props)
		{
			int ret = 0;
			BufEncFirmware fw = new BufEncFirmware();
			Properties subprops = new Properties
			{
				crypt_key = DEFAULT_KEY,
				magic = DEFAULT_MAGIC,
				seed = 0x4F   // Char: O
			};

			ToolsArgMap argMap = new ToolsArgMap();
			argMap.Init_args_BuffaloEnc(args, arg_idx, ref subprops);

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			if (CheckParams(subprops) != 0)
				return 1;

			fw.outFile = props.outFile;
			fw.outFMode = FileMode.Create;
			try
			{
				fw.inFs = new FileStream(props.inFile, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			if (fw.inFs.Length > uint.MaxValue) {
				Console.Error.WriteLine(Lang.Tools.BuffaloEncRes.Error_BigFile);
				return 1;
			}

			ret = subprops.isde ?
				Decrypt(ref fw, subprops, props) :
				Encrypt(ref fw, subprops, props);

			fw.inFs.Close();

			return ret;
		}
	}
}
