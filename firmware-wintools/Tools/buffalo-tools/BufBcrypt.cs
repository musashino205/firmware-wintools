using System;

namespace firmware_wintools.Tools
{
	static class BufBcrypt
	{
		public const int BCRYPT_DEFAULT_STATE_LEN = 256;
		public const int BCRYPT_MAX_KEYLEN = 254;

		public struct Bcrypt_ctx
		{
			public ulong i;
			public ulong j;
			public byte[] buf;
			public long buf_len;
		}

		static private int Bcrypt_Init(ref Bcrypt_ctx ctx, in byte[] key, int keylen,
			long state_len)
		{
			long k = 0;
			long i, j;

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

		static private void Bcrypt_Process(ref Bcrypt_ctx ctx, ref byte[] src,
			long offset, long len)
		{
			byte i, j;

			i = Convert.ToByte(ctx.i);
			j = Convert.ToByte(ctx.j);

			for (long k = 0; k < len; k++)
			{
				byte t;

				i = (byte)((i + 1) % ctx.buf_len);
				j = (byte)((j + ctx.buf[i]) % ctx.buf_len);
				t = ctx.buf[j];
				ctx.buf[j] = ctx.buf[i];
				ctx.buf[i] = t;

				if (offset >= 0)
					src[k] = (byte)(src[k + offset] ^ ctx.buf[(ctx.buf[i] + ctx.buf[j]) % ctx.buf_len]);
				else
					src[k + -offset] = (byte)(src[k + -offset] ^ ctx.buf[(ctx.buf[i] + ctx.buf[j]) % ctx.buf_len]);
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
		static internal int Bcrypt_Buf(byte seed, in byte[] key, ref byte[] src,
			long offset, long len, bool longstate)
		{
			byte[] bckey = new byte[BCRYPT_MAX_KEYLEN + 1];
			int keylen;
			Bcrypt_ctx ctx = new();

			ctx.buf = null;

			/* setup decryption key */
			keylen = key.Length;
			bckey[0] = seed;
			Array.Copy(key, 0, bckey, 1, keylen);

			// NULL終端済の場合はインクリメントしない
			if (key[key.Length - 1] != 0)
				keylen++;

			if (Bcrypt_Init(ref ctx, in bckey, keylen,
				longstate ? len : BCRYPT_DEFAULT_STATE_LEN) != 0)
				return 1;

			Bcrypt_Process(ref ctx, ref src, offset, len);

			return 0;
		}
	}
}
