using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace firmware_wintools.Tools
{
	class SenaoHeader : HeaderFooter
	{
		internal uint head = DEF_HEAD_VALUE;
		internal uint vendor_id;
		internal uint product_id;
		internal byte[] version = new byte[VER_LEN];
		internal uint firmware_type;
		internal uint filesize;
		internal uint zero = 0;
		internal byte[] md5sum = new byte[MD5_LEN];
		internal byte[] pad = new byte[PAD_LEN];
		internal uint cksum;
		internal uint magic = DEF_MAGIC;

		[NonSerialized]
		internal const int HDR_LEN = 0x60;

		[NonSerialized]
		internal const int VER_LEN = 0x10;

		[NonSerialized]
		internal const int MD5_LEN = 0x10;

		[NonSerialized]
		internal const int PAD_LEN = 0x20;

		[NonSerialized]
		internal const uint DEF_HEAD_VALUE = 0x0;

		[NonSerialized]
		internal const string DEF_VERSION = "123";

		[NonSerialized]
		internal const uint DEF_MAGIC = 0x12345678;

		[NonSerialized]
		internal static readonly
		Dictionary<byte, FirmwareType> FWTYPES = new Dictionary<byte, FirmwareType>()
		{
			{ 0x00, new FirmwareType(){ name = "combo", comment = "(not implemented)" } },	// 実装省略
			{ 0x01, new FirmwareType(){ name = "bootloader" } },
			{ 0x02, new FirmwareType(){ name = "kernel" } },
			{ 0x03, new FirmwareType(){ name = "kernelapp" } },
			{ 0x04, new FirmwareType(){ name = "apps" } },
			/* 以下メーカー依存の値 */
			{ 0x05, new FirmwareType(){ name = "littleapps", comment = "(D-Link)/factoryapps (EnGenius)" } },
			{ 0x06, new FirmwareType(){ name = "sounds", comment = "(D-Link)/littleapps (EnGenius)" } },
			{ 0x07, new FirmwareType(){ name = "userconfig", comment = "(D-Link)/appdata (EnGenius)" } },
			{ 0x08, new FirmwareType(){ name = "userconfig", comment = "(EnGenius)" } },
			{ 0x09, new FirmwareType(){ name = "odmapps", comment = "(EnGenius)" } },
			{ 0x0a, new FirmwareType(){ name = "factoryapps", comment = "(D-Link)" } },
			{ 0x0b, new FirmwareType(){ name = "odmapps", comment = "(D-Link)" } },
			{ 0x0c, new FirmwareType(){ name = "langpack", comment =  "(D-Link)" } }
		};

		/// <summary>
		/// ヘッダ部分のチェックサムを算出します。
		/// </summary>
		/// <param name="len">算出対象のデータ長</param>
		/// <returns>チェックサム値</returns>
		internal uint CalcHeaderCksum(long len)
		{
			uint sum = 0;
			byte[] buf = new byte[len];

			SerializeProps(ref buf, 0);

			for (long i = 0; i < len; ++i)
				sum += buf[i];

			return sum;
		}

		/// <summary>
		/// <code>firmware_type</code> の値から有効なファームウェアタイプであるかをチェックします。
		/// </summary>
		/// <returns>有効: true, 無効: false</returns>
		internal static bool ChkFwType(uint fw_type)
		{
			byte type = BitConverter.GetBytes(fw_type)[0];

			if (!FWTYPES.ContainsKey(type))
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkSenaoFwRes.Error_NoInvalidFwType);

				return false;
			}

			/* type = 0 ("combo") */
			if (fw_type == 0)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkSenaoFwRes.Error_NoImplCombo);
				return false;
			}

			return true;
		}

		internal class FirmwareType
		{
			internal string name;
			internal string comment = "";

			internal const byte TYPE_NONE = 0xFF;
		}
	}

	class SenaoFirmware : Firmware
	{
		internal SenaoHeader header;
		internal const int DEF_BLOCK_SIZE = 65535;

		/* common */
		internal void EncodeData(uint magic)
		{
			for (long i = 0; i < data.Length; i++)
				data[i] ^= (byte)(magic >> (int)(i % 8) & 0xff);
		}

		/* for encoding */
		internal byte[] GetMd5sum()
		{
			MD5CryptoServiceProvider md5p =
				new MD5CryptoServiceProvider();

			return md5p.ComputeHash(inFs);
		}

		/* for decoding */
		internal int LoadData(long length)
		{
			data = new byte[length];

			return (FileToBytes(in inFs, ref data, length) == length) ?
				0 : 1;
		}
	}
}
