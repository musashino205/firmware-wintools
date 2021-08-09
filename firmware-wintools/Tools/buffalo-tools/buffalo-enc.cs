using System;
using System.IO;
using System.Net;
using System.Text;

namespace firmware_wintools.Tools
{
	static class Buffalo_Enc
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
			public bool isMinorCksum;
			public bool force;
		}

		private static void PrintHelp(int arg_idx)
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

		private static void PrintInfo(Properties subprops, long datalen, uint cksum, bool isdbg)
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

		private static int CheckParams(Properties subprops)
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

		private static int Encrypt(ref BufEncFirmware fw, Properties subprops, Program.Properties props)
		{
			int ret;
			byte[] key;

			/* magic, product, versionは末尾 0x0 ('\0') で終端 */
			fw.header = new BufEncHeader()
			{
				magic = Encoding.ASCII.GetBytes(subprops.magic + "\0"),
				seed = subprops.seed,
				prod_len = subprops.product.Length + 1,
				product = Encoding.ASCII.GetBytes(subprops.product + "\0"),
				ver_len = subprops.version.Length + 1,
				version = Encoding.ASCII.GetBytes(subprops.version + "\0"),
				data_len = subprops.size > 0 ?
						(uint)subprops.size : (uint)fw.inFInfo.Length
			};

			fw.footer = new BufEncFooter();

			MemoryStream bufStream = new MemoryStream();

			/* キーは 0x00 ('\0') で終端 */
			key = Encoding.ASCII.GetBytes(subprops.crypt_key + "\0");

			/* ヘッダ/データ/フッタの各部長さ */
			fw.header.totalLen = fw.header.GetHeaderLen();	// ヘッダ
			fw.dataLen = fw.header.data_len;			// データ
			fw.footer.totalLen = sizeof(uint);			// フッタ1
			/* フッタ4byteパディング分 */
			fw.footer.totalLen +=				// フッタ2
				4 - (fw.footer.totalLen + fw.header.totalLen + fw.dataLen) % 4;

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					fw.data = new byte[fw.dataLen];
					Firmware.FileToBytes(in fw.inFs, ref fw.data, fw.dataLen);

					/*
					 * サイズ指定ある場合バッファにコピー
					 * バッファ側は読み込んだ分進むので0にシーク
					 */
					if (subprops.size > 0)
					{
						fw.inFs.CopyTo(bufStream);
						bufStream.Seek(0, SeekOrigin.Begin);
					}
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			fw.footer.cksum = fw.GetCksum(subprops.isMinorCksum);

			if (!props.quiet)
				PrintInfo(subprops, fw.dataLen, fw.footer.cksum, props.debug);

			/* ヘッダ暗号化（+ product長, ver長, データ長BE変換） */
			ret = fw.header.EncryptHeader(key, subprops.islong);
			if (ret > 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BuffaloEncRes.Error_FailEncryptHeader);
				return ret;
			}
			/* ヘッダシリアル化 */
			fw.headerData = new byte[fw.header.totalLen];
			if (fw.header.SerializeProps(ref fw.headerData, 0) != fw.header.totalLen)
				return 1;

			/* フッタcksumをBE変換 */
			fw.footer.cksum = (uint)IPAddress.HostToNetworkOrder((int)fw.footer.cksum);
			/* フッタシリアル化 */
			fw.footerData = new byte[fw.footer.totalLen];
			fw.footer.SerializeProps(ref fw.footerData, 0);

			/* データ暗号化 */
			if (fw.EncryptData(key, subprops.islong) > 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BuffaloEncRes.Error_FailEncryptData);
				return 1;
			}

			try
			{
				using (fw.outFs = new FileStream(fw.outFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
				{
					ret = fw.WriteToFile(false);

					/* サイズ指定ある場合残りのデータを出力ファイルにコピー */
					if (subprops.size > 0)
						bufStream.CopyTo(fw.outFs);
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			return ret;
		}

		private static int Decrypt(ref BufEncFirmware fw, Properties subprops, Program.Properties props)
		{
			int ret;
			uint cksum;
			byte[] key;

			fw.header = new BufEncHeader();
			fw.footer = new BufEncFooter();

			if (fw.inFInfo.Length < subprops.offset)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_LargeOffset,
					subprops.offset);
				return 1;
			}

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read)) {
					fw.inFs.Seek(subprops.offset, SeekOrigin.Begin);

					/* ヘッダ読み込み */
					ret = fw.header.LoadHeader(fw.inFs);
					if (ret > 0)
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.BuffaloEncRes.Error_FailLoadHeader);
						return ret;
					}

					/* データ読み込み */
					ret = fw.LoadData(fw.header.data_len);
					if (ret > 0)
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.BuffaloEncRes.Error_FailLoadData);
						return ret;
					}

					/* フッタ読み込み */
					ret = fw.footer.LoadFooter(fw.inFs);
					if (ret > 0)
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.BuffaloEncRes.Error_FailLoadFooter);
						return ret;
					}
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			key = Encoding.ASCII.GetBytes(subprops.crypt_key);

			/* Decrypt Header/Data */
			if (fw.header.DecryptHeader(key, subprops.islong) != 0 ||
			    fw.DecryptData(in key, subprops.islong) != 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BuffaloEncRes.Error_FailDecryptData);
				return 1;
			}

			fw.dataLen = fw.header.data_len;
			cksum = fw.GetCksum(subprops.isMinorCksum);
			/* 計算cksumと埋め込みcksum不一致 & 非forceならエラー */
			if (!subprops.force && cksum != fw.footer.cksum)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BuffaloEncRes.Error_CksumNotMatch);

				return 1;
			}
			if (cksum != fw.footer.cksum)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.BuffaloEncRes.Warn_CksumNotMatch);
			}

			subprops.magic = Encoding.ASCII.GetString(fw.header.magic).TrimEnd('\0');
			subprops.seed = fw.header.seed;
			subprops.product = Encoding.ASCII.GetString(fw.header.product).TrimEnd('\0');
			subprops.version = Encoding.ASCII.GetString(fw.header.version).TrimEnd('\0');
			subprops.size = Convert.ToInt32(fw.header.data_len);

			if (!props.quiet)
				PrintInfo(subprops, fw.header.data_len, fw.footer.cksum, props.debug);

			return fw.OpenAndWriteToFile(true);
		}

		public static int Do_BuffaloEnc(string[] args, int arg_idx, Program.Properties props)
		{
			int ret = 0;
			BufEncFirmware fw = new BufEncFirmware();
			Properties subprops = new Properties
			{
				crypt_key = DEFAULT_KEY,
				magic = DEFAULT_MAGIC,
				seed = 0x4F   // Char: O
			};

			ToolsArgMap.Init_args_BuffaloEnc(args, arg_idx, ref subprops);

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			if (CheckParams(subprops) != 0)
				return 1;

			fw.outFile = props.outFile;
			fw.outFMode = FileMode.Create;
			fw.inFInfo = new FileInfo(props.inFile);

			if (fw.inFInfo.Length > uint.MaxValue) {
				Console.Error.WriteLine(Lang.Tools.BuffaloEncRes.Error_BigFile);
				return 1;
			}

			ret = subprops.isde ?
				Decrypt(ref fw, subprops, props) :
				Encrypt(ref fw, subprops, props);

			if (ret != 0)
			{
				if (subprops.isde)
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix +
						Lang.Tools.BuffaloEncRes.Error_FailDecrypt);
				else
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix +
						Lang.Tools.BuffaloEncRes.Error_FailEncrypt);
			}

			return ret;
		}
	}
}
