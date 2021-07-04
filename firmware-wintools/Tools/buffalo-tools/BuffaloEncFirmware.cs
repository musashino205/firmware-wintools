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

		internal int LoadHeader(in FileStream fs)
		{
			byte[] buf = new byte[sizeof(int)];

			/* magic */
			magic = new byte[ENC_MAGIC_LEN];
			if (fs.Read(magic, 0, ENC_MAGIC_LEN) != ENC_MAGIC_LEN)
				return 1;
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
			product = new byte[prod_len];
			if (fs.Read(product, 0, prod_len) != prod_len)
				return 1;

			/* ver_len */
			if (fs.Read(buf, 0, sizeof(int)) != sizeof(int))
				return 1;
			ver_len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));

			/* version */
			version = new byte[ver_len];
			if (fs.Read(version, 0, ver_len) != ver_len)
				return 1;

			if (fs.Read(buf, 0, sizeof(uint)) != sizeof(uint))
				return 1;
			data_len = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));

			return 0;
		}

		internal int DecryptHeader(in byte[] key, bool longstate)
		{
			/* データの復号に使用するseedは復号前のversionの1文字目 */
			dataSeed = version[0];

			/* version */
			if (BufBcrypt.Bcrypt_Buf(product[0], in key, ref version, 0, ver_len, longstate) != 0)
				return 1;

			/* product */
			if (BufBcrypt.Bcrypt_Buf(seed, in key, ref product, 0, prod_len, longstate) != 0)
				return 1;

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

		internal uint GetCksum(bool minorCksum)
		{
			uint csum = (uint)dataLen;

			foreach (sbyte sbyteVal in data)
			{
				csum ^= minorCksum ?
					(byte)sbyteVal : (uint)sbyteVal;

				for (int i = 0; i < 8; i++)
					csum = ((csum & 1) > 0) ?
							(csum >> 1) ^ 0xEDB88320 :
							csum >>= 1;
			}

			return csum;
		}

		/* for encryption */
		internal int EncryptData(byte seed, in byte[] key, bool longstate)
		{
			return BufBcrypt.Bcrypt_Buf(seed, key, ref data, 0, dataLen, longstate);
		}

		/* for decryption */
		internal int LoadData(long length)
		{
			data = new byte[length];

			return (FileToBytes(in inFs, ref data, length) == length) ?
				0 : 1;
		}

		internal int DecryptData(long length, byte seed, in byte[] key, bool longstate)
		{
			return BufBcrypt.Bcrypt_Buf(seed, key, ref data, 0, length, longstate);
		}
	}
}
