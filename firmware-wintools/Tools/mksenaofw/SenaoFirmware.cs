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

		internal int LoadHeader(in FileStream fs)
		{
			byte[] buf = new byte[sizeof(uint)];

			/* head */
			if (fs.Read(buf, 0, sizeof(uint)) != sizeof(uint))
				return 1;
			head = BitConverter.ToUInt32(buf, 0);

			/* vendor_id */
			if (fs.Read(buf, 0, sizeof(uint)) != sizeof(uint))
				return 1;
			vendor_id = (uint)IPAddress.NetworkToHostOrder(
						BitConverter.ToInt32(buf, 0));

			/* product_id */
			if (fs.Read(buf, 0, sizeof(uint)) != sizeof(uint))
				return 1;
			product_id = (uint)IPAddress.NetworkToHostOrder(
						BitConverter.ToInt32(buf, 0));

			/* version */
			if (fs.Read(version, 0, VER_LEN) != VER_LEN)
				return 1;

			/* firmware_type */
			if (fs.Read(buf, 0, sizeof(uint)) != sizeof(uint))
				return 1;
			firmware_type = (uint)IPAddress.NetworkToHostOrder(
						BitConverter.ToInt32(buf, 0));

			/* filesize */
			if (fs.Read(buf, 0, sizeof(uint)) != sizeof(uint))
				return 1;
			filesize = (uint)IPAddress.NetworkToHostOrder(
						BitConverter.ToInt32(buf, 0));

			/* zero */
			if (fs.Length - fs.Position < sizeof(uint))
				return 1;
			fs.Seek(sizeof(uint), SeekOrigin.Current);

			/* md5sum */
			if (fs.Read(md5sum, 0, MD5_LEN) != MD5_LEN)
				return 1;

			/* pad */
			if (fs.Length - fs.Position < PAD_LEN)
				return 1;
			fs.Seek(PAD_LEN, SeekOrigin.Current);

			/* cksum */
			if (fs.Read(buf, 0, sizeof(uint)) != sizeof(uint))
				return 1;
			cksum = (uint)IPAddress.NetworkToHostOrder(
						BitConverter.ToInt32(buf, 0));

			/* magic */
			if (fs.Read(buf, 0, sizeof(uint)) != sizeof(uint))
				return 1;
			magic = (uint)IPAddress.NetworkToHostOrder(
						BitConverter.ToInt32(buf, 0));

			return 0;
		}

		/// <summary>
		/// <code>firmware_type</code> の値から有効なファームウェアタイプであるかをチェックします。
		/// </summary>
		/// <returns>有効: true, 無効: false</returns>
		internal static bool ChkFwType(uint fw_type)
		{
			byte type = BitConverter.GetBytes(fw_type)[0];

			if (!FirmwareType.TYPES.ContainsKey(type))
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
			internal const byte TYPE_NONE = 0xFF;

			internal static readonly
			Dictionary<byte, TypeInfo> TYPES = new Dictionary<byte, TypeInfo>()
			{
			{ 0x00, new TypeInfo(){ name = "combo", comment = "(not implemented)" } },	// 実装省略
			{ 0x01, new TypeInfo(){ name = "bootloader" } },
			{ 0x02, new TypeInfo(){ name = "kernel" } },
			{ 0x03, new TypeInfo(){ name = "kernelapp" } },
			{ 0x04, new TypeInfo(){ name = "apps" } },
			/* 以下メーカー依存の値 */
			{ 0x05, new TypeInfo(){ name = "littleapps", comment = "(D-Link)/factoryapps (EnGenius)" } },
			{ 0x06, new TypeInfo(){ name = "sounds", comment = "(D-Link)/littleapps (EnGenius)" } },
			{ 0x07, new TypeInfo(){ name = "userconfig", comment = "(D-Link)/appdata (EnGenius)" } },
			{ 0x08, new TypeInfo(){ name = "userconfig", comment = "(EnGenius)" } },
			{ 0x09, new TypeInfo(){ name = "odmapps", comment = "(EnGenius)" } },
			{ 0x0a, new TypeInfo(){ name = "factoryapps", comment = "(D-Link)" } },
			{ 0x0b, new TypeInfo(){ name = "odmapps", comment = "(D-Link)" } },
			{ 0x0c, new TypeInfo(){ name = "langpack", comment =  "(D-Link)" } }
			};

			internal class TypeInfo
			{
				internal string name;
				internal string comment = "";
			}
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
			return MD5.Create().ComputeHash(inFs);
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
