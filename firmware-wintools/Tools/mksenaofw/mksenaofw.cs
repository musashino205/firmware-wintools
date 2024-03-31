using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace firmware_wintools.Tools
{
	internal class MkSenaoFw : Tool
	{
		/* ツール情報　*/
		public override string name { get => "mksenaofw"; }
		public override string desc { get => Lang.Tools.MkSenaoFwRes.FuncDesc; }
		public override string descFmt { get => Lang.Tools.MkSenaoFwRes.Main_FuncDesc_Fmt; }
		public override string resName => "MkSenaoFwRes";


		private byte FWType = SenaoHeader.FirmwareType.TYPE_NONE;
		private byte[] Version = Encoding.ASCII.GetBytes(SenaoHeader.DEF_VERSION);
		private uint Vendor = 0;
		private uint Product = 0;
		private uint Magic = SenaoHeader.DEF_MAGIC;
		private bool IsDec = false;
		private bool Pad = false;
		private int BS = SenaoFirmware.DEF_BLOCK_SIZE;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 't', PType = Param.PTYPE.BYTE, SetField = "FWType" },
			new Param() { PChar = 'v', PType = Param.PTYPE.BARY, SetField = "Version" },
			new Param() { PChar = 'r', PType = Param.PTYPE.UINT, SetField = "Vendor" },
			new Param() { PChar = 'p', PType = Param.PTYPE.UINT, SetField = "Product" },
			new Param() { PChar = 'm', PType = Param.PTYPE.UINT, SetField = "Magic" },
			new Param() { PChar = 'z', PType = Param.PTYPE.BOOL, SetField = "Pad" },
			new Param() { PChar = 'b', PType = Param.PTYPE.INT, SetField = "BS" },
			new Param() { PChar = 'd', PType = Param.PTYPE.BOOL, SetField = "IsDec" }
		};

		private static new void PrintHelp(int arg_idx)
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
			foreach (KeyValuePair<byte, SenaoHeader.FirmwareType> pair
					in SenaoHeader.FWTYPES)
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

		private void PrintInfo(byte[] md5)
		{
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info,
				IsDec ?
					Lang.Tools.MkSenaoFwRes.Info_Decode :
					Lang.Tools.MkSenaoFwRes.Info_Encode);
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_FwType,
				FWType,
				SenaoHeader.FWTYPES[FWType].name);
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_FwVer,
				Encoding.ASCII.GetString(Version));
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_Vendor, Vendor);
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_Product, Product);
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_MD5,
				BitConverter.ToString(md5).Replace("-", ""));
			Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_Magic, Magic);

			if (!IsDec)
			{
				Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_Pad, Pad);
				if (Pad)
					Console.WriteLine(Lang.Tools.MkSenaoFwRes.Info_BS,
						BS);
			}
		}

		/// <summary>
		/// 渡されたパラメータを基にしてデータのエンコードとヘッダの付加を行います。
		/// </summary>
		/// <param name="fw">SenaoFirmwareクラス</param>
		/// <param name="props">メインプロパティ</param>
		/// <param name="subprops">機能プロパティ</param>
		/// <returns></returns>
		private int Encode(ref SenaoFirmware fw, Program.Properties props)
		{
			long pad_len;

			fw.header = new SenaoHeader()
			{
				vendor_id = (uint)Utils.BE32toHost(Vendor),
				product_id = (uint)Utils.BE32toHost(Product),
				firmware_type = (uint)Utils.BE32toHost(FWType),
				filesize = (uint)IPAddress.HostToNetworkOrder((int)fw.inFInfo.Length),
				magic = (uint)Utils.BE32toHost(Magic),
				totalLen = SenaoHeader.HDR_LEN
			};

			/* versionをコピー */
			Array.Copy(Version, fw.header.version,
				Version.Length > SenaoHeader.VER_LEN ?
					SenaoHeader.VER_LEN : Version.Length);

			/* パディングサイズ */
			pad_len = Pad & BS > 0 ? (BS - (fw.inFInfo.Length % BS)) : 0;

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					fw.header.md5sum = fw.GetMd5sum();
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

			fw.header.cksum = (uint)Utils.BE32toHost(
					fw.header.CalcHeaderCksum(SenaoHeader.HDR_LEN));

			if (!props.Quiet)
				PrintInfo(fw.header.md5sum);

			/* ヘッダシリアル化 */
			fw.headerData = new byte[SenaoHeader.HDR_LEN];
			if (fw.header.SerializeProps(ref fw.headerData, 0) != SenaoHeader.HDR_LEN)
				return 1;

			/* データエンコード */
			fw.EncodeData(Magic);

			return fw.OpenAndWriteToFile(false);
		}

		/// <summary>
		/// 渡されたパラメータを基に、データのデコードを行います。
		/// </summary>
		/// <param name="fw">SenaoFirmwareクラス</param>
		/// <param name="props">メインプロパティ</param>
		/// <param name="subprops">機能プロパティ</param>
		/// <returns></returns>
		private int Decode(ref SenaoFirmware fw, Program.Properties props)
		{
			fw.header = new SenaoHeader();

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					if (fw.header.LoadHeader(in fw.inFs) != 0)
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.MkSenaoFwRes.Error_FailLoadHeader);
						return 1;
					}

					if (fw.LoadData(fw.header.filesize) != 0)
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

			if (fw.header.filesize > int.MaxValue)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkSenaoFwRes.Error_LargeInFile);
				return 1;
			}

			FWType = BitConverter.GetBytes(fw.header.firmware_type)[0];
			Vendor = fw.header.vendor_id;
			Product = fw.header.product_id;
			Version = fw.header.version;
			Magic = fw.header.magic;

			if (!props.Quiet)
				PrintInfo(fw.header.md5sum);

			if (!SenaoHeader.ChkFwType(fw.header.firmware_type))
				return 1;

			fw.EncodeData(fw.header.magic);

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
		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			SenaoFirmware fw = new SenaoFirmware();
			int ret;

			if (props.Help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return ret;

			fw.inFInfo = new FileInfo(props.InFile);

			/* エンコード時 */
			if (!IsDec)
			{
				if (!SenaoHeader.ChkFwType(Convert.ToUInt32(FWType)))
					return 1;

				/* 0 < version len < 17 */
				if (Version.Length > SenaoHeader.VER_LEN ||
				    Version.Length == 0)
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
						Lang.Tools.MkSenaoFwRes.Error_InvalidVerLen);
					return 1;
				}

				/* ref: https://wikidevi.com/wiki/Senao */
				if (Vendor == 0 || Product == 0)
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

			fw.outFile = props.OutFile;
			fw.outFMode = FileMode.Create;

			return IsDec ?
					Decode(ref fw, props) :
					Encode(ref fw, props);
		}
	}
}
