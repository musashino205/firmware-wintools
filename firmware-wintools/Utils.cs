using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace firmware_wintools
{
	internal static class Utils
	{
		public static string GetStrParamFromArg(string[] args, int index)
		{
			if (index + 1 < args.Length &&
			    !args[index + 1].StartsWith("-") && args[index + 1].Length > 0)
				return args[index + 1];
			else
				return null;
		}

		/// <summary>
		/// オプションに指定された文字列値の引数を読み取り、 <paramref name="value"/> に
		/// 代入して返します
		/// </summary>
		/// <param name="args"></param>
		/// <param name="index"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool GetStrParam(string[] args, int index, out string value)
			=> (value = GetStrParamFromArg(args, index)) != null;

		/// <summary>
		/// オプションに指定された文字列値の引数を読み取り、有効な文字列であれば
		/// <paramref name="value"/> に上書きして返します
		/// </summary>
		/// <param name="args"></param>
		/// <param name="index"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool GetStrParamOrKeep(string[] args, int index, ref string value)
		{
			string tmp;

			tmp = GetStrParamFromArg(args, index);
			if (tmp == null || tmp.Length <= 0)
				return false;

			value = tmp;
			return true;
		}

		public static bool StrToLong(string valStr, out long cnv, NumberStyles styles)
		{
			int suf = 1;
			CultureInfo provider = CultureInfo.CurrentCulture;
			cnv = 0;

			if (valStr == null)
				return false;

			switch (styles)
			{
				case NumberStyles.None:     // use as "auto"
					/* jump to "default" if no hex prefix */
					if (!valStr.StartsWith("0x"))
						goto default;
					valStr = valStr.Remove(0, 2);
					styles = NumberStyles.HexNumber;
					break;
				case NumberStyles.HexNumber:    // do nothing
					break;
				default:
					styles = NumberStyles.Integer;
					break;
			}

			if (styles != NumberStyles.HexNumber)
			{
				switch (valStr[valStr.Length - 1])
				{
					case 'g':
					case 'G':
						suf *= 1024;
						goto case 'M';
					case 'm':
					case 'M':
						suf *= 1024;
						goto case 'K';
					case 'k':
					case 'K':
						suf *= 1024;
						valStr = valStr.Remove(valStr.Length - 1, 1);
						break;
				}
			}

			if (!long.TryParse(valStr, styles, provider, out cnv))
				return false;

			cnv *= suf;

			return true;
		}

		public static bool StrToInt(string val, out int cnv, NumberStyles numstyle)
		{
			cnv = 0;

			if (!StrToLong(val, out long cnvLong, numstyle) ||
				cnvLong > int.MaxValue ||
				cnvLong < int.MinValue)
				return false;

			cnv = (int)cnvLong;

			return true;
		}

		public static bool StrToUInt(string val, out uint cnv, NumberStyles numstyle)
		{
			cnv = 0;

			if (!StrToLong(val, out long cnvLong, numstyle) ||
				cnvLong > uint.MaxValue ||
				cnvLong < uint.MinValue)
				return false;

			cnv = (uint)cnvLong;

			return true;
		}

		public static bool
		StrToByteArray(ref string val, out byte[] cnv, int offset, int bytes)
		{
			CultureInfo provider = CultureInfo.InvariantCulture;
			cnv = null;
			string c;

			if (bytes * 2 > val.Length)
				return false;

			cnv = new byte[bytes];

			for (int i = 0; i < bytes; i++)
			{
				c = val.Substring(offset + i * 2, 2);
				if (!byte.TryParse(c, NumberStyles.HexNumber, provider, out cnv[i]))
				{
					val = c;
					return false;
				}
			}

			return true;
		}

		public static bool StrToByteArray(ref string val, out byte[] cnv)
		{
			cnv = null;

			if (val.Length % 2 != 0)
				return false;

			return StrToByteArray(ref val, out cnv, 0, val.Length / 2);
		}

		public static short BE16toHost(short val)
			=> System.Net.IPAddress.NetworkToHostOrder(val);

		public static short BE16toHost(ushort val)
			=> BE16toHost((short)val);

		public static int BE32toHost(int val)
		{
			return System.Net.IPAddress.NetworkToHostOrder(val);
		}

		public static int BE32toHost(uint val)
		{
			return BE32toHost((int)val);
		}

		public static long BE64toHost(long val)
		{
			return System.Net.IPAddress.NetworkToHostOrder(val);
		}

		public static short LE16toHost(short val)
		{
			byte[] ary;

			if (BitConverter.IsLittleEndian)
				return val;

			ary = BitConverter.GetBytes(val);
			Array.Reverse(ary);
			return BitConverter.ToInt16(ary, 0);
		}

		public static short LE16toHost(ushort val)
			=> LE16toHost((short)val);

		public static int LE32toHost(int val)
		{
			byte[] ary;

			if (BitConverter.IsLittleEndian)
				return val;

			ary = BitConverter.GetBytes(val);
			Array.Reverse(ary);
			return BitConverter.ToInt32(ary, 0);
		}

		public static int LE32toHost(uint val)
		{
			return LE32toHost((int)val);
		}

		public static long LE64toHost(long val)
		{
			byte[] ary;

			if (BitConverter.IsLittleEndian)
				return val;

			ary = BitConverter.GetBytes(val);
			Array.Reverse(ary);
			return BitConverter.ToInt32(ary, 0);
		}

		public static DateTime UnixZeroUTC()
		{
			return new DateTime(1970, 1, 1, 0, 0, 0,
					DateTimeKind.Utc);
		}

		public static DateTime UnixToUTC(uint unixtime)
		{
			return UnixZeroUTC().AddSeconds(unixtime);
		}

		public static int
		XorData(ref byte[] data, int len, in byte[] pattern, int p_off)
		{
			for (int i = 0; i < len; i++, p_off %= pattern.Length)
				data[i] ^= pattern[p_off++];

			return p_off;
		}

		public static void
		AesData(int keylen, in byte[] key, in byte[] iv, bool encrypt,
			CipherMode cipher, PaddingMode pad,
			in Stream input, in Stream output, long datalen)
		{
			using (Aes aes = Aes.Create())
			{
				aes.Key = key;
				aes.IV = iv;
				aes.KeySize = keylen;
				aes.Mode = cipher;
				aes.Padding = pad;

				using (CryptoStream cs
					= new CryptoStream(
						output,
						encrypt ?
							aes.CreateEncryptor(key, iv) :
							aes.CreateDecryptor(key, iv),
						CryptoStreamMode.Write))
				{
					byte[] buf = new byte[0x10000];
					int readlen;

					while ((readlen = input.Read(buf, 0, buf.Length)) > 0)
					{
						if (datalen > readlen)
						{
							datalen -= readlen;
							cs.Write(buf, 0, readlen);
						}
						else
						{
							cs.Write(buf, 0, Convert.ToInt32(datalen));
							break;
						}
					}
				}
			}
		}

		public static byte[]
		ZlibDecompress(in byte[] buf, int offset, int cmplen, int ucmplen)
		{
			byte[] output = new byte[ucmplen];
			byte[] zlibhdr = new byte[2];
			int readlen;

			try
			{
				using (MemoryStream ms = new MemoryStream(buf, offset, cmplen))
				{
					if ((readlen = ms.Read(zlibhdr, 0, zlibhdr.Length)) != zlibhdr.Length)
						return null;
					/*
					 * Zlib header (RFC1950, 2.2 Data format):
					 *
					 * - zlibhdr[0]: CMF (Compression Method and flags)
					 *   - bits 7:4 CINFO (compression info)
					 *   - bits 3:0 CM (compression method)
					 *
					 * - zlibhdr[1]: FLG (Flags)
					 *   - bits 7:6 FLEVEL (compression level)
					 *   - bit  5   FDICT (preset dictionary)
					 *   - bits 4:0 FCHECK (check bits for CMF and FLG)
					 */
					if (zlibhdr[0] != 0x78 ||
						(zlibhdr[1] & 0x20) == 1 ||
						(zlibhdr[0] * 0x100 + zlibhdr[1]) % 0x1f != 0)
						return null;

					using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
						ds.Read(output, 0, output.Length);
				}
			}
			catch (Exception)
			{
				return null;
			}

			return output;
		}

		public static byte[]
		ZlibDecompress(in byte[] buf, int ucmplen)
			=> ZlibDecompress(buf, 0, buf.Length, ucmplen);
	}
}
