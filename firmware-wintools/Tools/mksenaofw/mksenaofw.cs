using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace firmware_wintools.Tools
{
	class MkSenaoFw
	{
		const int HDR_LEN = 0x60;
		const int BUF_SIZE = 0x200;
		const int VERSION_SIZE = 0x10;
		const int MD5_SIZE = 0x10;
		const int PAD_SIZE = 0x20;

		const int DEFAULT_BLOCK_SIZE = 65535;

		const int DEFAULT_HEAD_VALUE = 0x0;
		const string DEFAULT_VERSION = "123";
		const int DEFAULT_MAGIC = 0x12345678;

		byte[] md5sum;

		/// <summary>
		/// mksenaofwの機能プロパティ
		/// </summary>
		public struct Properties
		{
			/// <summary>
			/// decodeモードか否か
			/// </summary>
			internal bool isde;
			/// <summary>
			/// ファームウェアタイプ
			/// </summary>
			internal byte fw_type;
			/// <summary>
			/// ファームウェアバージョン文字列
			/// </summary>
			internal string version;
			/// <summary>
			/// ベンダID
			/// </summary>
			internal uint vendor;
			/// <summary>
			/// プロダクトID
			/// </summary>
			internal uint product;
			/// <summary>
			/// ファームウェアマジック値
			/// </summary>
			internal uint magic;
			/// <summary>
			/// ファームウェア末尾パディング有無
			/// </summary>
			internal bool pad;
			/// <summary>
			/// パディング時のブロックサイズ
			/// </summary>
			internal int bs;
		}

		/// <summary>
		/// Senaoヘッダ構造体
		/// </summary>
		public struct Header
		{
			internal uint head;
			internal uint vendor_id;
			internal uint product_id;
			internal byte[] version;
			internal uint fw_type;
			internal int filesize;
			internal uint zero;
			internal byte[] md5sum;
			internal byte[] pad;
			internal uint cksum;
			internal uint magic;
		}

		/// <summary>
		/// ファームウェアタイプ配列
		/// </summary>
		static readonly Firmware_Type[] FIRMWARE_TYPES = new Firmware_Type[]
		{
			new Firmware_Type() { id = 0x00, name = "combo", comment = "(not implemented)" },	// 実装省略
			new Firmware_Type() { id = 0x01, name = "bootloader" },
			new Firmware_Type() { id = 0x02, name = "kernel" },
			new Firmware_Type() { id = 0x03, name = "kernelapp" },
			new Firmware_Type() { id = 0x04, name = "apps" },
			/* 以下メーカー依存の値 */
			new Firmware_Type() { id = 0x05, name = "littleapps (D-Link)/factoryapps (EnGenius)" },
			new Firmware_Type() { id = 0x06, name = "sounds (D-Link)/littleapps (EnGeinus)" },
			new Firmware_Type() { id = 0x07, name = "userconfig (D-Link)/appdata (EnGeinus)" },
			new Firmware_Type() { id = 0x08, name = "userconfig (EnGeinus)" },
			new Firmware_Type() { id = 0x09, name = "odmapps (EnGeinus)" },
			new Firmware_Type() { id = 0x0a, name = "factoryapps (D-Link)" },
			new Firmware_Type() { id = 0x0b, name = "odmapps (D-Link)" },
			new Firmware_Type() { id = 0x0c, name = "langpack (D-Link)" }
		};

		/// <summary>
		/// ファームウェアタイプ未指定値
		/// </summary>
		const byte FIRMEARE_TYPE_NONE = 0xFF;

		private void PrintHelp()
		{
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Help_Usage +
				Lang.Tools.MkSenaoFwRes.FuncDesc +
				Environment.NewLine);
			// 共通オプション表示
			Program.PrintCommonOption();
			// 機能オプション表示
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Help_Options +
				Lang.Tools.MkSenaoFwRes.Help_Options_t +
				Lang.Tools.MkSenaoFwRes.Help_Options_t_values);
			for (int i = 0; i < FIRMWARE_TYPES.Length; i++)     // firmware types
			{
				Console.WriteLine(Lang.Tools.MkSenaoFwRes.Help_Options_t_TypeFmt,
					FIRMWARE_TYPES[i].id < 10 ?
						FIRMWARE_TYPES[i].id.ToString() + " " : FIRMWARE_TYPES[i].id.ToString(),
					FIRMWARE_TYPES[i].name,
					FIRMWARE_TYPES[i].comment);
			}
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Help_Options_t_Line +
				Lang.Tools.MkSenaoFwRes.Help_Options_v +
				Lang.Tools.MkSenaoFwRes.Help_Options_r +
				Lang.Tools.MkSenaoFwRes.Help_Options_p +
				Lang.Tools.MkSenaoFwRes.Help_Options_m +
				Lang.Tools.MkSenaoFwRes.Help_Options_z +
				Lang.Tools.MkSenaoFwRes.Help_Options_b +
				Lang.Tools.MkSenaoFwRes.Help_Options_d);
		}

		private void PrintInfo(Properties subprops)
		{
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info,
				subprops.isde ? Lang.Tools.MkSenaoFwRes.Info_Decode: Lang.Tools.MkSenaoFwRes.Info_Encode);
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_FwType,
				subprops.fw_type, FIRMWARE_TYPES[subprops.fw_type].name);
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_FwVer, subprops.version);
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_Vendor, subprops.vendor);
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_Product, subprops.product);
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_MD5,
				BitConverter.ToString(md5sum).Replace("-", ""));
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_Magic, subprops.magic);

			if (!subprops.isde)
			{
				Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_Pad, subprops.pad);
				if (subprops.pad)
					Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_BS,
						subprops.bs);
			}

		}

		/// <summary>
		/// <paramref name="fw_type"/> の値から有効なファームウェアタイプであるかをチェックします。
		/// </summary>
		/// <param name="fw_type"></param>
		/// <returns>有効: 0, 無効: 1</returns>
		private int ChkFwType(uint fw_type)
		{
			if (fw_type > FIRMWARE_TYPES.Length - 1 ||	// 渡されたtypeが配列長を超過
				fw_type == FIRMEARE_TYPE_NONE)			// または未指定状態
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix + 
					Lang.Tools.MkSenaoFwRes.Error_NoInvalidFwType);
				return 1;
			}
			else if (fw_type == 0)						// typeが "combo"
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkSenaoFwRes.Error_NoImplCombo);
				return 1;
			}

			return 0;
		}

		/// <summary>
		/// ヘッダ部分のチェックサムを算出します。
		/// </summary>
		/// <param name="data">算出対象データ</param>
		/// <param name="len">算出対象のデータ長</param>
		/// <returns>算出成功: チェックサム値, 失敗: -1</returns>
		private int Calc_HeaderCksum(in byte[] data, int len)
		{
			int sum = 0;

			if (data == null || len <= 0)
				return -1;

			for (int i = 0; i < len; ++i)			
				sum += data[i];

			return sum;
		}

		/// <summary>
		/// 渡されたパラメータを基にしてデータのエンコードとヘッダの付加を行います。
		/// </summary>
		/// <param name="subprops">mksenaofw機能プロパティ</param>
		/// <param name="inFs">入力ファイルのFileStream</param>
		/// <param name="outFs">出力ファイルのFileStream</param>
		/// <returns>成功: 0, 失敗: 1</returns>
		private int Encode(Properties subprops, ref FileStream inFs, ref FileStream outFs, bool quiet)
		{
			int filesize, cksum, read_len, pad_len, avail_len;
			byte[] buf = new byte[BUF_SIZE];
			MD5CryptoServiceProvider md5p = new MD5CryptoServiceProvider();
			Header fw_header = new Header()
			{
				head = DEFAULT_HEAD_VALUE,
				vendor_id = (uint)IPAddress.HostToNetworkOrder((int)subprops.vendor),
				product_id = (uint)IPAddress.HostToNetworkOrder((int)subprops.product),
				version = Encoding.ASCII.GetBytes(subprops.version),
				fw_type = (uint)IPAddress.HostToNetworkOrder((int)subprops.fw_type),
				//filesize
				zero = 0,
				md5sum = md5p.ComputeHash(inFs),
				pad = new byte[PAD_SIZE],
				//cksum
				magic = (uint)IPAddress.HostToNetworkOrder((int)subprops.magic)
			};

			/* MD5CryptoServiceProvider.ComputeHash(Stream)でPositionが末尾まで飛ぶ */
			inFs.Position = 0;

			if (inFs.Length > 0x7FFFFFFFL)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkSenaoFwRes.Error_LargeInFile);
				return 1;
			}
			filesize = Convert.ToInt32(inFs.Length);

			md5sum = fw_header.md5sum;

			if (!quiet)
				PrintInfo(subprops);

			/* パディングサイズ */
			pad_len = subprops.pad && subprops.bs > 0 ? 
				subprops.bs - (filesize % subprops.bs) : 0;

			fw_header.filesize = IPAddress.HostToNetworkOrder(filesize);

			Array.Copy(BitConverter.GetBytes(fw_header.vendor_id), 0,
				buf, sizeof(uint), sizeof(uint));
			Array.Copy(BitConverter.GetBytes(fw_header.product_id), 0, 
				buf, sizeof(uint) * 2, sizeof(uint));
			Array.Copy(fw_header.version, 0, buf, sizeof(uint) * 3, fw_header.version.Length);
			Array.Copy(BitConverter.GetBytes(fw_header.fw_type), 0,
				buf, sizeof(uint) * 7, sizeof(uint));
			Array.Copy(BitConverter.GetBytes(fw_header.filesize), 0,
				buf, sizeof(uint) * 8, sizeof(uint));
			Array.Copy(fw_header.md5sum, 0, buf, sizeof(uint) * 10, MD5_SIZE);
			/* Checksumの算出/コピーはMagicをbufにコピーした後 */
			Array.Copy(BitConverter.GetBytes(fw_header.magic), 0,
				buf, HDR_LEN - sizeof(uint), sizeof(uint));

			if ((cksum = Calc_HeaderCksum(in buf, HDR_LEN)) < 0)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkSenaoFwRes.Error_FailHeaderCksum);
				return 1;
			}
			fw_header.cksum = (uint)IPAddress.HostToNetworkOrder(cksum);
			Array.Copy(BitConverter.GetBytes(fw_header.cksum), 0,
				buf, HDR_LEN - sizeof(uint) * 2, sizeof(uint));

			/* ヘッダ書き込み */
			outFs.Write(buf, 0, HDR_LEN);

			/* データをencodeして書き込み */
			while ((read_len = inFs.Read(buf, 0, BUF_SIZE)) > 0 || pad_len > 0)
			{
				if (read_len < BUF_SIZE && pad_len > 0)	// bufに余りがありパディングが残っている
				{
					avail_len = BUF_SIZE - read_len;
					Array.Clear(buf, read_len, avail_len);
					read_len += avail_len < pad_len ? avail_len : pad_len;
					pad_len -= avail_len < pad_len ? avail_len : pad_len;
				}

				for (int i = 0; i < read_len; i++)
					buf[i] ^= (byte)(subprops.magic >> (i % 8) & 0xff);
				outFs.Write(buf, 0, read_len);
			}

			return 0;
		}

		/// <summary>
		/// 渡されたパラメータを基に、データのデコードを行います。
		/// </summary>
		/// <param name="inFs">入力ファイルのFileStream</param>
		/// <param name="outFs">出力ファイルのFileStream</param>
		/// <returns>成功: 0, 失敗: 1</returns>
		private int Decode(ref FileStream inFs, ref FileStream outFs, bool quiet)
		{
			int read_len, written_len = 0;
			byte[] buf = new byte[BUF_SIZE];
			Properties subprops = new Properties()
			{
				isde = true
			};
			Header fw_header = new Header()
			{
				version = new byte[VERSION_SIZE],
				md5sum = new byte[MD5_SIZE]
			};

			inFs.Read(buf, 0, HDR_LEN);

			fw_header.head = BitConverter.ToUInt32(buf, 0);
			fw_header.vendor_id = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, sizeof(uint)));
			fw_header.product_id = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, sizeof(uint) * 2));
			Array.Copy(buf, sizeof(uint) * 3, fw_header.version, 0, VERSION_SIZE);
			fw_header.fw_type =
				(uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, sizeof(uint) * 3 + VERSION_SIZE));
			fw_header.filesize = 
				IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, sizeof(uint) * 4 + VERSION_SIZE));
			Array.Copy(buf, HDR_LEN - sizeof(uint) * 2 - PAD_SIZE - MD5_SIZE, fw_header.md5sum, 0, MD5_SIZE);
			fw_header.cksum =
				(uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, HDR_LEN - sizeof(uint) * 2));
			fw_header.magic =
				(uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, HDR_LEN - sizeof(uint)));

			subprops.fw_type = BitConverter.GetBytes(fw_header.fw_type)[0];
			subprops.vendor = fw_header.vendor_id;
			subprops.product = fw_header.product_id;
			subprops.version = Encoding.ASCII.GetString(fw_header.version);
			subprops.magic = fw_header.magic;
			md5sum = fw_header.md5sum;

			if (!quiet)
				PrintInfo(subprops);
			if (ChkFwType(fw_header.fw_type) != 0)
				return 1;

			while ((read_len = inFs.Read(buf, 0, BUF_SIZE)) > 0)
			{
				for (int i = 0; i < read_len; i++)
					buf[i] ^= (byte)(fw_header.magic >> (i % 8) & 0xff);

				if (written_len + read_len > fw_header.filesize) // 最後パディングされている場合
				{
					read_len = fw_header.filesize - written_len;
					if (read_len > 0)
						outFs.Write(buf, 0, read_len);
					break;
				}

				outFs.Write(buf, 0, read_len);
				written_len += read_len;
			}

			return 0;
		}

		/// <summary>
		/// mksenaofwメイン関数
		/// <para>コマンドライン引数とメインプロパティから、各プロパティの構成と
		/// エンコード/デコード への分岐を行います。</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">メインプロパティ</param>
		/// <returns>成功: 0, 失敗: 1</returns>
		public int Do_MkSenaoFw(string[] args, Program.Properties props)
		{
			Properties subprops = new Properties()
			{
				fw_type = FIRMEARE_TYPE_NONE,
				version = DEFAULT_VERSION,
				magic = DEFAULT_MAGIC,
				bs = DEFAULT_BLOCK_SIZE
			};

			if (props.help)
			{
				PrintHelp();
				return 0;
			}

			ToolsArgMap argMap = new ToolsArgMap();
			argMap.Init_args_MkSenaoFw(args, ref subprops);

			if (!subprops.isde)	// エンコード時
			{
				if (ChkFwType(Convert.ToUInt32(subprops.fw_type)) != 0)
					return 1;

				if (Encoding.ASCII.GetByteCount(subprops.version) > sizeof(uint) * 4 ||	// 0 < version len < 17
					Encoding.ASCII.GetByteCount(subprops.version) == 0)
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
						Lang.Tools.MkSenaoFwRes.Error_InvalidVerLen);
					return 1;
				}

				/* ref: https://wikidevi.com/wiki/Senao */
				if (subprops.vendor == 0 || subprops.product == 0)
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
						Lang.Tools.MkSenaoFwRes.Error_NoInvalidVenProd);
					return 1;
				}
			}

			FileStream inFs;
			FileStream outFs;
			FileMode outFMode =
				File.Exists(props.outFile) ? FileMode.Truncate : FileMode.Create;
			try
			{
				inFs = new FileStream(props.inFile, FileMode.Open, FileAccess.Read, FileShare.Read);
				outFs = new FileStream(props.outFile, outFMode, FileAccess.Write, FileShare.None);
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			int ret = subprops.isde ?
				Decode(ref inFs, ref outFs, props.quiet) :
				Encode(subprops, ref inFs, ref outFs, props.quiet);

			inFs.Close();
			outFs.Close();

			return ret;
		}
	}


}
