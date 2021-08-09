using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace firmware_wintools.Tools
{
	class MkSenaoFw
	{
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

		private void PrintHelp(int arg_idx)
		{
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Help_Usage +
				Lang.Tools.MkSenaoFwRes.FuncDesc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");	// 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption();
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				Lang.Tools.MkSenaoFwRes.Help_Options_t +
				Lang.Tools.MkSenaoFwRes.Help_Options_t_values);
			foreach (KeyValuePair<byte, SenaoHeader.FirmwareType.TypeInfo> pair
					in SenaoHeader.FirmwareType.TYPES)
			{
				Console.WriteLine(Lang.Tools.MkSenaoFwRes.Help_Options_t_TypeFmt,
					pair.Key,
					pair.Value.name,
					pair.Value.comment);
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
				subprops.isde ?
					Lang.Tools.MkSenaoFwRes.Info_Decode :
					Lang.Tools.MkSenaoFwRes.Info_Encode);
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_FwType,
				subprops.fw_type,
				SenaoHeader.FirmwareType.TYPES[subprops.fw_type].name);
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
		/// 渡されたパラメータを基にしてデータのエンコードとヘッダの付加を行います。
		/// </summary>
		/// <param name="fw">SenaoFirmwareクラス</param>
		/// <param name="props">メインプロパティ</param>
		/// <param name="subprops">機能プロパティ</param>
		/// <returns></returns>
		private int Encode(ref SenaoFirmware fw, Program.Properties props, Properties subprops)
		{
			long pad_len;

			SenaoHeader header = new SenaoHeader()
			{
				vendor_id = (uint)IPAddress.HostToNetworkOrder((int)subprops.vendor),
				product_id = (uint)IPAddress.HostToNetworkOrder((int)subprops.product),
				firmware_type = (uint)IPAddress.HostToNetworkOrder((int)subprops.fw_type),
				filesize = (uint)IPAddress.HostToNetworkOrder((int)fw.inFInfo.Length),
				magic = (uint)IPAddress.HostToNetworkOrder((int)subprops.magic),
				totalLen = SenaoHeader.HDR_LEN
			};

			/* versionをコピー */
			Array.Copy(
				Encoding.ASCII.GetBytes(subprops.version),
				header.version,
				Encoding.ASCII.GetByteCount(subprops.version) > SenaoHeader.VER_LEN ?
					SenaoHeader.VER_LEN :
					Encoding.ASCII.GetByteCount(subprops.version));

			/* パディングサイズ */
			pad_len = subprops.bs > 0 ?
					(subprops.bs - (fw.inFInfo.Length % subprops.bs)) : 0;

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					md5sum = header.md5sum = fw.GetMd5sum();
					/* MD5sum読み取りでStreamのPositionが末尾まで飛ぶ */
					fw.inFs.Seek(0, SeekOrigin.Begin);

					/* パディング分もencodeする為fw.dataで扱う */
					fw.data = new byte[fw.inFInfo.Length + pad_len];
					Firmware.FileToBytes(in fw.inFs, ref fw.data, fw.inFInfo.Length);
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			header.cksum = (uint)IPAddress.HostToNetworkOrder(
						(int)header.CalcHeaderCksum(SenaoHeader.HDR_LEN));

			if (!props.quiet)
				PrintInfo(subprops);

			/* ヘッダシリアル化 */
			fw.header = new byte[SenaoHeader.HDR_LEN];
			if (header.SerializeProps(ref fw.header, 0) != SenaoHeader.HDR_LEN)
				return 1;

			/* データエンコード */
			fw.EncodeData(subprops.magic);

			return fw.OpenAndWriteToFile(false);
		}

		/// <summary>
		/// 渡されたパラメータを基に、データのデコードを行います。
		/// </summary>
		/// <param name="fw">SenaoFirmwareクラス</param>
		/// <param name="props">メインプロパティ</param>
		/// <param name="subprops">機能プロパティ</param>
		/// <returns></returns>
		private int Decode(ref SenaoFirmware fw, Program.Properties props, Properties subprops)
		{
			SenaoHeader header = new SenaoHeader();

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					if (header.LoadHeader(in fw.inFs) != 0)
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.MkSenaoFwRes.Error_FailLoadHeader);
						return 1;
					}

					if (fw.LoadData(header.filesize) != 0)
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.MkSenaoFwRes.Error_FailLoadData);
						return 1;
					}
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			if (header.filesize > int.MaxValue)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkSenaoFwRes.Error_LargeInFile);
				return 1;
			}

			subprops.fw_type = BitConverter.GetBytes(header.firmware_type)[0];
			subprops.vendor = header.vendor_id;
			subprops.product = header.product_id;
			subprops.version = Encoding.ASCII.GetString(header.version);
			subprops.magic = header.magic;
			md5sum = header.md5sum;

			if (!props.quiet)
				PrintInfo(subprops);

			if (!SenaoHeader.ChkFwType(header.firmware_type))
				return 1;

			fw.EncodeData(header.magic);

			return fw.OpenAndWriteToFile(true);
		}

		/// <summary>
		/// mksenaofwメイン関数
		/// <para>コマンドライン引数とメインプロパティから、各プロパティの構成と
		/// エンコード/デコード への分岐を行います。</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">メインプロパティ</param>
		/// <returns>成功: 0, 失敗: 1</returns>
		public int Do_MkSenaoFw(string[] args, int arg_idx, Program.Properties props)
		{
			SenaoFirmware fw = new SenaoFirmware();
			Properties subprops = new Properties()
			{
				fw_type = SenaoHeader.FirmwareType.TYPE_NONE,
				version = SenaoHeader.DEF_VERSION,
				magic = SenaoHeader.DEF_MAGIC,
				bs = SenaoFirmware.DEF_BLOCK_SIZE
			};

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ToolsArgMap argMap = new ToolsArgMap();
			argMap.Init_args_MkSenaoFw(args, arg_idx, ref subprops);

			if (!subprops.pad)
				subprops.bs = 0;

			fw.inFInfo = new FileInfo(props.inFile);

			/* エンコード時 */
			if (!subprops.isde)
			{
				if (!SenaoHeader.ChkFwType(Convert.ToUInt32(subprops.fw_type)))
					return 1;

				/* 0 < version len < 17 */
				if (Encoding.ASCII.GetByteCount(subprops.version) > SenaoHeader.VER_LEN ||
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

				/*
				 * decode時はfilesizeの長さしか扱わない
				 * .NETのArrayで扱える要素数はintの範囲内
				 */
				if (fw.inFInfo.Length > int.MaxValue)
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
						Lang.Tools.MkSenaoFwRes.Error_LargeInFile);
					return 1;
				}
			}

			fw.outFile = props.outFile;
			fw.outFMode = FileMode.Create;

			return subprops.isde ?
					Decode(ref fw, props, subprops) :
					Encode(ref fw, props, subprops);
		}
	}


}
