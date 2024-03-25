using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace firmware_wintools.Tools
{
	internal class Buffalo_Enc : Tool
	{
		/* ツール情報　*/
		public override string name { get => "buffalo-enc"; }
		public override string desc { get => Lang.Tools.BuffaloEncRes.FuncDesc; }
		public override string descFmt { get => Lang.Tools.BuffaloEncRes.Main_FuncDesc_Fmt; }
		public override string resName => "BuffaloEncRes";

		private string CryptKey = "Buffalo";
		private string Magic = "start";
		private byte Seed = 0x4F; /* Char: 'O' */
		private string Product = null;
		private string Version = null;
		private long Length = -1;
		private long Offset = 0;
		private bool IsDec = false;
		private bool IsLong = false;
		private bool IsMinorCksum = false;
		private bool Force = false;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 'd', PType = Param.PTYPE.BOOL, SetField = "IsDec", HelpKey = "Help_Options_d" },
			new Param() { PChar = 'l', PType = Param.PTYPE.BOOL, SetField = "IsLong", HelpKey = "Help_Options_l" },
			new Param() { PChar = 'k', PType = Param.PTYPE.STR, SetField = "CryptKey", HelpKey = "Help_Options_k", HelpOpt = CryptKey },
			new Param() { PChar = 'm', PType = Param.PTYPE.STR, SetField = "Magic", HelpKey = "Help_Options_m", HelpOpt = Magic },
			new Param() { PChar = 'p', PType = Param.PTYPE.STR, SetField = "Product", HelpKey = "Help_Options_p" },
			new Param() { PChar = 'v', PType = Param.PTYPE.STR, SetField = "Version", HelpKey = "Help_Options_v" },
			new Param() { PChar = 's', PType = Param.PTYPE.BYTE, SetField = "Seed" },
			new Param() { PChar = 'O', PType = Param.PTYPE.LONG, SetField = "Offset", HelpKey = "Help_Options_o2" },
			new Param() { PChar = 'S', PType = Param.PTYPE.LONG, SetField = "Length", HelpKey = "Help_Options_S" },
			new Param() { PChar = 'C', PType = Param.PTYPE.BOOL, SetField = "IsMinorCksum" },
			new Param() { PChar = 'F', PType = Param.PTYPE.BOOL, SetField = "Force", HelpKey = "Help_Options_F" }
		};

		private void PrintInfo(uint cksum)
		{
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info,
				IsDec ?
					Lang.Tools.BuffaloEncRes.Info_Decrypt :
					Lang.Tools.BuffaloEncRes.Info_Encrypt);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Magic, Magic);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Seed, Seed);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Product, Product);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Version, Version);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_DataLen, Length);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Cksum, cksum);
		}

		private int Encrypt(ref BufEncFirmware fw, Program.Properties props)
		{
			MemoryStream ms = new MemoryStream();
			byte[] key;
			/* 暗号化しないデータ長 */
			long noEncLen = fw.inFInfo.Length - Length;
			int ret;

			/*
			 * 以下はNULL終端
			 * - Magic
			 * - Product
			 * - Version
			 */
			fw.header = new BufEncHeader()
			{
				magic = Encoding.ASCII.GetBytes(Magic + "\0"),
				seed = Seed,
				product = Encoding.ASCII.GetBytes(Product + "\0"),
				version = Encoding.ASCII.GetBytes(Version + "\0"),
				prod_len = Product.Length + 1,
				ver_len = Version.Length + 1,
				data_len = (uint)Length
			};
			fw.footer = new BufEncFooter();

			/* NULL終端 */
			key = Encoding.ASCII.GetBytes(CryptKey + "\0");

			fw.header.totalLen = fw.header.GetHeaderLen();
			fw.dataLen = fw.header.data_len;
			fw.footer.totalLen = sizeof(uint);
			/* 4byteブロックパディング */
			fw.footer.totalLen
				+= 4 - (fw.header.totalLen + fw.footer.totalLen + fw.dataLen) % 4;

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
						FileAccess.Read, FileShare.Read))
				{
					fw.data = new byte[fw.dataLen];
					Firmware.FileToBytes(fw.inFs, ref fw.data, fw.dataLen);

					/* 残データをMemoryStreamにコピー */
					if (noEncLen > 0)
					{
						fw.inFs.CopyTo(ms);
						ms.Seek(0, SeekOrigin.Begin);
					}
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			fw.footer.cksum = fw.GetCksum(IsMinorCksum);

			if (!props.quiet)
				PrintInfo(fw.footer.cksum);

			ret = fw.header.EncryptHeader(key, IsLong);
			if (ret != 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BuffaloEncRes.Error_FailEncryptHeader);
				return ret;
			}

			fw.footer.cksum = (uint)Utils.BE32toHost(fw.footer.cksum);

			fw.headerData = new byte[fw.header.totalLen];
			fw.footerData = new byte[fw.footer.totalLen];

			if (fw.header.SerializeProps(ref fw.headerData, 0)
					!= fw.header.totalLen)
				return 1;
			fw.footer.SerializeProps(ref fw.footerData, 0);

			ret = fw.EncryptData(key, IsLong);
			if (ret != 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BuffaloEncRes.Error_FailEncryptData);
				return ret;
			}

			try
			{
				using (fw.outFs = new FileStream(fw.outFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
				{
					ret = fw.WriteToFile(false);

					/* 残データコピー */
					if (noEncLen > 0)
						ms.CopyTo(fw.outFs);
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			return ret;
		}

		private int Decrypt(ref BufEncFirmware fw, Program.Properties props)
		{
			byte[] key;
			uint cksum;
			int ret;

			if (Offset > fw.inFInfo.Length)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BuffaloEncRes.Error_LargeOffset);
				return 1;
			}

			fw.header = new BufEncHeader();
			fw.footer = new BufEncFooter();

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					fw.inFs.Seek(Offset, SeekOrigin.Begin);

					ret = fw.header.LoadHeader(fw.inFs);
					if (ret != 0)
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.BuffaloEncRes.Error_FailLoadHeader);
						return 1;
					}

					ret = fw.LoadData(fw.header.data_len);
					if (ret != 0)
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.BuffaloEncRes.Error_FailLoadData);
						return 1;
					}

					ret = fw.footer.LoadFooter(fw.inFs);
					if (ret != 0)
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.BuffaloEncRes.Error_FailLoadFooter);
						return 1;
					}
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			key = Encoding.ASCII.GetBytes(CryptKey);

			ret = fw.header.DecryptHeader(key, IsLong);
			if (ret == 0)
				ret = fw.DecryptData(key, IsLong);
			if (ret != 0)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BuffaloEncRes.Error_FailDecryptData);
				return ret;
			}

			fw.dataLen = fw.header.data_len;
			cksum = fw.GetCksum(IsMinorCksum);
			if (cksum != fw.footer.cksum)
			{
				Console.Error.WriteLine(
					(Force ?
						Lang.Resource.Main_Warning_Prefix :
						Lang.Resource.Main_Error_Prefix) +
					Lang.Tools.BuffaloEncRes.WarnError_CksumNotMatch);
				if (!Force)
					return 1;
			}

			Magic = Encoding.ASCII.GetString(fw.header.magic).TrimEnd('\0');
			Seed = fw.header.seed;
			Product = Encoding.ASCII.GetString(fw.header.product).TrimEnd('\0');
			Version = Encoding.ASCII.GetString(fw.header.version).TrimEnd('\0');
			Length = fw.header.data_len;

			if (!props.quiet)
				PrintInfo(cksum);

			return fw.OpenAndWriteToFile(true);
		}

		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			BufEncFirmware fw = new BufEncFirmware();
			int ret;

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return ret;

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			if (CryptKey.Length > BufBcrypt.BCRYPT_MAX_KEYLEN) {
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BuffaloEncRes.Error_LongKey);
				return 1;
			}

			if (Magic.Length != BufEncHeader.ENC_MAGIC_LEN - 1) {
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BuffaloEncRes.Error_InvalidMagicLen);
				return 1;
			}

			if (!IsDec)
			{
				if (Product == null || Version == null)
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
						Product == null ?
							Lang.Tools.BuffaloEncRes.Error_NoProduct :
							Lang.Tools.BuffaloEncRes.Error_NoVersion);
					return 1;
				}
				if (Product.Length > BufEncHeader.ENC_PRODUCT_LEN - 1 ||
				    Version.Length > BufEncHeader.ENC_VERSION_LEN - 1) {
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
						(Product.Length > (BufEncHeader.ENC_PRODUCT_LEN - 1) ?
							Lang.Tools.BuffaloEncRes.Error_LongProduct :
							Lang.Tools.BuffaloEncRes.Error_LongVersion));
					return 1;
				}
			}

			fw.outFile = props.outFile;
			fw.outFMode = FileMode.Create;
			fw.inFInfo = new FileInfo(props.inFile);

			if (!IsDec && Length == -1)
				Length = fw.inFInfo.Length;
			/*
			 * サイズチェック
			 *
			 * - 復号かつデータ長 - オフセットが uint 最大値より長い
			 * - 暗号化かつ対象データ長が uint 最大値より長い
			 */
			if ((IsDec && fw.inFInfo.Length - Offset > uint.MaxValue) ||
			    (!IsDec && Length > uint.MaxValue))
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BuffaloEncRes.Error_BigFile);
				return 1;
			}

			ret = IsDec ?
				Decrypt(ref fw, props) :
				Encrypt(ref fw, props);
			if (ret != 0) {
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					(IsDec ?
						Lang.Tools.BuffaloEncRes.Error_FailDecrypt :
						Lang.Tools.BuffaloEncRes.Error_FailEncrypt));
			}

			return ret;
		}
	}
}
