using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace firmware_wintools.Tools
{
	partial class Buffalo_Lib
	{
		private int Bcrypt_Init(ref Bcrypt_ctx ctx, ref byte[] key, uint keylen,
			ulong state_len)
		{
			ulong k = 0;
			ulong i, j;

			ctx.buf = new byte[state_len];

			ctx.i = ctx.j = 0;
			ctx.buf_len = state_len;

			for (i = 0; i < state_len; i++)
				//ctx.buf[i] = Convert.ToByte(i);
			ctx.buf[i] = (byte)i;

			for (i = 0, j = 0; i < state_len; i++, j = (j + 1) % keylen)
			{
				byte t;

				t = ctx.buf[i];
				k = (k + key[j] + t) % state_len;
				ctx.buf[i] = ctx.buf[k];
				ctx.buf[k] = t;
			}

			return 0;
		}

		private void Bcrypt_Process(ref Bcrypt_ctx ctx, ref byte[] src,
			long offset, ulong len)
		{
			byte i, j;

			i = Convert.ToByte(ctx.i);
			j = Convert.ToByte(ctx.j);

			for (ulong k = 0; k < len; k++)
			{
				byte t;

				i = (byte)((uint)(i + 1) % ctx.buf_len);
				j = (byte)((uint)(j + ctx.buf[i]) % ctx.buf_len);
				t = ctx.buf[j];
				ctx.buf[j] = ctx.buf[i];
				ctx.buf[i] = t;

				if (offset >= 0)
					src[k] = (byte)(src[k + (ulong)offset] ^ ctx.buf[(uint)(ctx.buf[i] + ctx.buf[j]) % ctx.buf_len]);
				else
					src[k + (ulong)-offset] = (byte)(src[k + (ulong)-offset] ^ ctx.buf[(uint)(ctx.buf[i] + ctx.buf[j]) % ctx.buf_len]);
			}

			ctx.i = i;
			ctx.j = j;
		}

		/// <summary>
		/// 渡されたパラメータを用いて <paramref name="src"/> をcryptします。
		/// </summary>
		/// <remarks>offsetについて
		///   <para>
		///     0: バッファに対し演算開始位置0、結果の挿入開始位置 0<br />
		///     正の値: バッファに対し演算開始位置 <paramref name="offset"/> 、結果の挿入位置 0<br />
		///     負の値: バッファに対し演算開始位置 <paramref name="offset"/> 、結果の挿入位置 <paramref name="offset"/>
		///   </para>
		/// </remarks>
		/// <param name="seed">拡張キーの生成に用いるseed値</param>
		/// <param name="key">基本キー</param>
		/// <param name="src">対象バッファ</param>
		/// <param name="offset">オフセット</param>
		/// <param name="len"></param>
		/// <param name="longstate"></param>
		/// <returns>成功: 0, 失敗: 1</returns>
		private int Bcrypt_Buf(byte seed, ref byte[] key, ref byte[] src,
			long offset, ulong len, bool longstate)
		{
			byte[] bckey = new byte[BCRYPT_MAX_KEYLEN + 1];
			uint keylen;
			Bcrypt_ctx ctx = new Bcrypt_ctx();

			ctx.buf = null;

			/* setup decryption key */
			keylen = Convert.ToUInt32(key.Length);
			bckey[0] = seed;
			Array.Copy(key, 0, bckey, 1, keylen);

			// NULL終端済の場合はインクリメントしない
			if (key[key.Length - 1] != 0)
				keylen++;

			if (Bcrypt_Init(ref ctx, ref bckey, keylen,
				longstate ? len : BCRYPT_DEFAULT_STATE_LEN) != 0)
				return 1;

			Bcrypt_Process(ref ctx, ref src, offset, len);

			return 0;
		}

		public uint Buffalo_Csum(uint csum, ref byte[] buf, ulong offset, ulong len)
		{
			// ref: https://blog.ch3cooh.jp/entry/20111005/1317772085
			sbyte[] tmp = ((buf as Array) as sbyte[]);

			for (ulong i = 0; i < len; i++)
			{
				csum ^= (uint)tmp[i + offset];
				for (int j = 0; j < 8; j++)
					csum = (uint)((csum >> 1) ^ (((csum & 1) > 0) ? 0xEDB88320ul : 0));
			}

			return csum;
		}

		/// <summary>
		/// <paramref name="product"/> と <paramref name="version"/> からヘッダ長を算出します。</summary>
		/// <remarks><paramref name="product"/> と <paramref name="version"/> は予めNULL終端しているため、
		/// その分の +1 は行わない。</remarks>
		/// <param name="product">productのバイト配列</param>
		/// <param name="version">versionのバイト配列</param>
		/// <returns>ヘッダ長</returns>
		public ulong Enc_Compute_HeaderLen(ref byte[] product, ref byte[] version)
		{
			return (ulong)ENC_MAGIC_LEN + 1 + (ulong)product.Length +
				(ulong)version.Length + 3 * sizeof(uint);
		}

		public ulong Enc_Compute_BufLen(ref byte[] product, ref byte[] version, ulong datalen)
		{
			ulong ret;

			ret = Enc_Compute_HeaderLen(ref product, ref version);
			ret += datalen + sizeof(uint);
			ret += (4 - ret % 4);

			return ret;
		}

		private int Check_Magic(ref byte[] buf, int offset)
		{
			byte[] magic_buf = new byte[ENC_MAGIC_LEN - 1];

			Array.Copy(buf, offset, magic_buf, 0, ENC_MAGIC_LEN - 1);

			if (Enumerable.SequenceEqual(Encoding.ASCII.GetBytes("start"), magic_buf) || 
				Enumerable.SequenceEqual(Encoding.ASCII.GetBytes("asar1"), magic_buf))
				return 0;
			else
				return 1;
		}

		private int CHECKLEN(ref uint len, uint inLen, long remain)
		{
			len = inLen;

			return remain < len ? 1 : 0;
		}

		private void INCP(ref int p_off, uint len, ref long remain)
		{
			p_off += Convert.ToInt32(len);
			remain -= len;
		}

		public int Encrypt_Buf(ref Enc_Param ep, ref byte[] data, long hdrlen)
		{
			int offset = 0;
			int len;
			byte s;

			/* setup Magic */
			len = ep.magic.Length;
			Array.Copy(ep.magic, 0, data, offset, len);
			offset += len;

			/* setup Seed */
			data[offset] = ep.seed;
			offset++;

			/* put "Product" len. */
			len = ep.product.Length;
			uint tmp = (uint)IPAddress.NetworkToHostOrder(len);
			Array.Copy(BitConverter.GetBytes(tmp), 0, data, offset, sizeof(uint));
			offset += sizeof(uint);

			/* copy and crypt Product name */
			Array.Copy(ep.product, 0, data, offset, len);
			if (Bcrypt_Buf(
				ep.seed, ref ep.key, ref data, -offset, Convert.ToUInt64(len), ep.longstate)
				!= 0)
				return 1;
			s = data[offset];
			offset += len;

			/* put "Version" len. */
			len = ep.version.Length;
			Array.Copy(BitConverter.GetBytes((uint)IPAddress.NetworkToHostOrder(len)),
				0, data, offset, sizeof(uint));
			offset += sizeof(uint);

			/* copy and crypt Version */
			Array.Copy(ep.version, 0, data, offset, len);
			if (Bcrypt_Buf(
				s, ref ep.key, ref data, -offset, Convert.ToUInt64(len), ep.longstate) != 0)
				return 1;
			s = data[offset];
			offset += len;

			/* put data length */
			Array.Copy(
				BitConverter.GetBytes(IPAddress.NetworkToHostOrder((int)ep.datalen)), 0,
				data, offset, sizeof(uint));
			offset += sizeof(uint);

			/* encrypt data */
			if (Bcrypt_Buf(s, ref ep.key, ref data, -offset, ep.datalen, ep.longstate) != 0)
				return 1;
			offset += (int)ep.datalen;

			/* put Checksum */
			Array.Copy(
				BitConverter.GetBytes(IPAddress.NetworkToHostOrder((int)ep.cksum)), 0,
				data, offset, sizeof(uint));

			return 0;
		}

		/// <summary>
		/// バッファを指定された引数によりdecryptします。
		/// </summary>
		/// <param name="ep">encryptパラメータ構造体</param>
		/// <param name="data">decrypt対象データ</param>
		/// <param name="data_off">decrypt開始offset</param>
		/// <param name="data_len">decrypt対象データ長（offset関係なくデータの長さ）</param>
		/// <returns></returns>
		public int Decrypt_Buf(ref Enc_Param ep, ref byte[] data, long data_len)
		{
			int offset = 0;
			uint prod_len;
			uint ver_len;
			uint len = 0;
			uint cksum;
			long remain;

			remain = data_len;

			/* Magic */
			if (CHECKLEN(ref len, ENC_MAGIC_LEN, remain) != 0)
				return 1;
			if (Check_Magic(ref data, offset) != 0)
				return 1;
			Array.Copy(data, offset, ep.magic, 0, ENC_MAGIC_LEN);
			INCP(ref offset, len, ref remain);

			/* Seed */
			if (CHECKLEN(ref len, 1, remain) != 0)
				return 1;
			ep.seed = data[offset];
			INCP(ref offset, len, ref remain);

			/* product len. */
			if (CHECKLEN(ref len, sizeof(uint), remain) != 0)
				return 1;
			prod_len =
				(uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(data, offset));
			if (prod_len > ENC_PRODUCT_LEN)
				return 1;
			INCP(ref offset, len, ref remain);

			/* Product */
			if (CHECKLEN(ref len, prod_len, remain) != 0)
				return 1;
			Array.Copy(data, offset, ep.product, 0, prod_len);
			INCP(ref offset, len, ref remain);

			/* version len. */
			if (CHECKLEN(ref len, sizeof(uint), remain) != 0)
				return 1;
			ver_len =
				(uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(data, offset));
			if (ver_len > ENC_VERSION_LEN)
				return 1;
			INCP(ref offset, len, ref remain);

			/* Version */
			if (CHECKLEN(ref len, ver_len, remain) != 0)
				return 1;
			Array.Copy(data, offset, ep.version, 0, ver_len);
			INCP(ref offset, len, ref remain);

			/* Data Len. */
			if (CHECKLEN(ref len, sizeof(uint), remain) != 0)
				return 1;
			ep.datalen =
				(uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(data, offset));
			INCP(ref offset, len, ref remain);

			/* decrypt data */
			if (CHECKLEN(ref len, ep.datalen, remain) != 0)
				return 1;
			if (
				Bcrypt_Buf(ep.version[0], ref ep.key, ref data, offset, ep.datalen, ep.longstate)
				!= 0)
				return 1;
			INCP(ref offset, len, ref remain);

			/* Checksum */
			if (CHECKLEN(ref len, sizeof(uint), remain) != 0)
				return 1;
			ep.cksum =
				(uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(data, offset));
			INCP(ref offset, len, ref remain);

			/* calc and compare checksum */
			cksum = Buffalo_Csum(ep.datalen, ref data, 0, ep.datalen);
			if (cksum != ep.cksum)
				return 1;

			/* decrypt "Version" */
			if (Bcrypt_Buf(ep.product[0], ref ep.key, ref ep.version, 0, ver_len, ep.longstate) != 0)
				return 1;

			/* decrypt "Product" */
			if (Bcrypt_Buf(ep.seed, ref ep.key, ref ep.product, 0, prod_len, ep.longstate) != 0)
				return 1;

			return 0;
		}
	}
}
