using System;
using System.IO;
using System.Net;
using System.Text;

namespace firmware_wintools.Tools
{
	class BufEncHeader : HeaderFooter
	{
		internal byte[] magic;
		internal byte seed;
		internal int prod_len;
		internal byte[] product;
		internal int ver_len;
		internal byte[] version;
		internal uint data_len;

		/* データ用seed（product, version処理後のもの） */
		[NonSerialized]
		internal byte dataSeed;

		[NonSerialized]
		public const int ENC_PRODUCT_LEN = 32;
		/// <summary>
		/// versionの長さ
		/// <para>文字列7文字 + NULL文字1文字 = 長さ8</para>
		/// </summary>
		[NonSerialized]
		public const int ENC_VERSION_LEN = 8;
		/// <summary>
		/// magicの長さ
		/// <para>文字列5文字 + NULL文字1文字 = 長さ6</para>
		/// </summary>
		[NonSerialized]
		public const int ENC_MAGIC_LEN = 6;

		/* for encryption */
		internal int GetHeaderLen()
		{
			return ENC_MAGIC_LEN			// magic
				+ 1				// seed
				+ prod_len			// product
				+ ver_len			// version
				+ sizeof(int) * 2		// prod_len + ver_len
				+ sizeof(uint);			// data_len
		}

		internal int EncryptHeader(in byte[] key, bool longstate)
		{
			int ret;

			ret = BufBcrypt.Bcrypt_Buf(seed, key, ref product, 0, prod_len, longstate);
			if (ret > 0)
				return ret;
			/* version用seedは処理済productの1文字目(index: 0) */
			dataSeed = product[0];
			BufBcrypt.Bcrypt_Buf(dataSeed, key, ref version, 0, ver_len, longstate);
			if (ret > 0)
				return ret;
			/* data用seedは処理済versionの1文字目(index: 0) */
			dataSeed = version[0];

			/* LE -> BE */
			prod_len = IPAddress.HostToNetworkOrder(prod_len);
			ver_len = IPAddress.HostToNetworkOrder(ver_len);
			/* HostToNetworkOrder(long taret)で認識されると0が返る為intにキャスト */
			data_len = (uint)IPAddress.HostToNetworkOrder((int)data_len);

			return 0;
		}

		/* for decryption */
		bool CheckMagic(byte[] magic)
		{
			string magicStr = Encoding.ASCII.GetString(magic).TrimEnd('\0');

			return magicStr.Equals("start") || magicStr.Equals("asar1");
		}

		internal int LoadHeader(in FileStream fs, in byte[] key, bool longstate)
		{
			byte[] buf = new byte[128];

			/* magic */
			if (fs.Read(buf, 0, ENC_MAGIC_LEN) != ENC_MAGIC_LEN)
				return 1;

			magic = new byte[ENC_MAGIC_LEN];
			Array.Copy(buf, magic, ENC_MAGIC_LEN);
			if (!CheckMagic(magic))
				return 1;

			/* seed */
			if ((fs.Read(buf, 0, 1)) != 1)
				return 1;
			seed = buf[0];

			/* prod_len */
			if (fs.Read(buf, 0, sizeof(int)) != sizeof(int))
				return 1;
			/* BitConverter.ToInt32で頭4byteのみ使われて後は落ちる */
			prod_len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));

			/* product */
			if (fs.Read(buf, 0, prod_len) != prod_len)
				return 1;
			product = new byte[prod_len];
			Array.Copy(buf, product, prod_len);
			BufBcrypt.Bcrypt_Buf(seed, in key, ref product, 0, prod_len, longstate);
			dataSeed = buf[0];

			/* ver_len */
			if (fs.Read(buf, 0, sizeof(int)) != sizeof(int))
				return 1;
			ver_len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));

			/* version */
			if (fs.Read(buf, 0, ver_len) != ver_len)
				return 1;
			version = new byte[ver_len];
			Array.Copy(buf, version, ver_len);
			BufBcrypt.Bcrypt_Buf(dataSeed, in key, ref version, 0, ver_len, longstate);
			dataSeed = buf[0];

			if (fs.Read(buf, 0, sizeof(uint)) != sizeof(uint))
				return 1;
			data_len = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));

			return 0;
		}
	}

	class BufEncFooter : HeaderFooter
	{
		internal uint cksum;

		/* for decryption */
		internal int LoadFooter(in FileStream fs)
		{
			byte[] buf = new byte[sizeof(uint)];

			if (fs.Read(buf, 0, sizeof(uint)) != sizeof(uint))
				return 1;
			cksum = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));

			return 0;
		}
	}

	class BufEncFirmware : Firmware
	{
		/* 実際のデータ長（パディング分を含まない） */
		internal long dataLen;

		internal uint GetCksum()
		{
			// ref: https://blog.ch3cooh.jp/entry/20111005/1317772085
			sbyte[] tmp = ((data as Array) as sbyte[]);
			uint csum = (uint)dataLen;

			for (long i = 0; i < dataLen; i++)
			{
				csum ^= (uint)tmp[i];
				for (int j = 0; j < 8; j++)
					csum = (uint)((csum >> 1) ^ (((csum & 1) > 0) ? 0xEDB88320ul : 0));
			}

			return csum;
		}

		/* for encryption */
		internal int EncryptData(byte seed, in byte[] key, bool longstate)
		{
			return BufBcrypt.Bcrypt_Buf(seed, key, ref data, 0, dataLen, longstate);
		}

		/* for decryption */
		internal int LoadData(long length, byte seed, in byte[] key, bool longstate)
		{
			data = new byte[length];
			if (FileToBytes(in inFs, ref data, length) != length)
				return 1;

			return BufBcrypt.Bcrypt_Buf(seed, key, ref data, 0, length, longstate);
		}
	}
}
