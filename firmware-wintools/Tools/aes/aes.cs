using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace firmware_wintools.Tools
{
	internal partial class Aes : Tool
	{
		/* ツール情報　*/
		public override string name { get => "aes"; }
		public override string desc { get => Lang.Tools.AesRes.FuncDesc; }
		public override string descFmt { get => Lang.Tools.AesRes.Main_FuncDesc_Fmt; }

		public struct Properties
		{
			public string iv;
			public string key;
			public bool hex_iv;
			public bool hex_key;
			public int keylen;
			public long offset;
			public string len;
			public bool decrypt;
		}

		private static void PrintHelp(int arg_idx)
		{
			Console.WriteLine(Lang.Tools.AesRes.Help_Usage +
				Lang.Tools.AesRes.FuncDesc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");	// 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption();
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				Lang.Tools.AesRes.Help_Options_d +
				Lang.Tools.AesRes.Help_Options_k +
				Lang.Tools.AesRes.Help_Options_K2 +
				Lang.Tools.AesRes.Help_Options_v +
				Lang.Tools.AesRes.Help_Options_V2 +
				Lang.Tools.AesRes.Help_Options_l +
				Lang.Tools.AesRes.Help_Options_O2 +
				Lang.Tools.AesRes.Help_Options_s);
		}

		private static void PrintInfo(Properties subprops, byte[] key, byte[] iv, long filelen)
		{
			Console.WriteLine(Lang.Tools.AesRes.Info,		// mode info
				subprops.decrypt ?
				Lang.Tools.AesRes.Info_Decrypt :
				Lang.Tools.AesRes.Info_Encrypt);
			Console.WriteLine(					// aes mode (128/256)
				Lang.Tools.AesRes.Info_mode,
				"AES-" + subprops.keylen + "-CBC");
			if (subprops.hex_key)					// key
				Console.WriteLine(
					Lang.Tools.AesRes.Info_key2,
					BitConverter.ToString(key).Replace("-", ""));
			else
				Console.WriteLine(
					Lang.Tools.AesRes.Info_key,
					subprops.key,
					BitConverter.ToString(key).Replace("-", ""));
			if (subprops.hex_iv || subprops.iv == null)
				Console.WriteLine(				// iv
					Lang.Tools.AesRes.Info_iv2,
					BitConverter.ToString(iv).Replace("-", ""));
			else
				Console.WriteLine(
					Lang.Tools.AesRes.Info_iv,
					subprops.iv,
					BitConverter.ToString(iv).Replace("-", ""));
			Console.WriteLine(					// length
				Lang.Tools.AesRes.Info_len,
				filelen);
			Console.WriteLine(					// offset
				Lang.Tools.AesRes.Info_offset,
				subprops.offset);
		}

		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			byte[] iv;
			byte[] key;
			int keylen;
			long offset = 0;
			long len = 0;
			Properties subprops = new Properties()
			{
				keylen = 256,
			};
			CryptoStream Cs;
			Firmware fw = new Firmware();

			Init_args(args, arg_idx, ref subprops);

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			fw.inFInfo = new FileInfo(props.inFile);
			fw.outFile = props.outFile;

			keylen = subprops.keylen;

			iv = new byte[16];
			key = new byte[keylen / 8];	// keylenはbit値、128または256

			/*
			 * check/build iv and key
			 */
			/* iv */
			if (subprops.iv == null || subprops.iv.Length == 0)	// if iv is not specified or blank
			{
				/* use default array of iv (filled by '0') */
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.AesRes.Warning_NoIV);
			}
			else	// if iv is specified
			{
				if ((!subprops.hex_iv && subprops.iv.Length > iv.Length) ||
				    (subprops.hex_iv && subprops.iv.Length != iv.Length * 2))
				{

					if (subprops.hex_iv)
						Console.Error.Write(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.AesRes.Error_InvalidIVLenHex);
					else
						Console.Error.Write(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.AesRes.Error_LongIVLen);

					return 1;
				}

				if (subprops.hex_iv)
				{
					if (!Utils.StrToByteArray(ref subprops.iv, out iv))
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.AesRes.Error_InvalidIVHex);
						if (iv != null)
							Console.Error.WriteLine("(char: \"{0}\")", subprops.iv);

						return 1;
					}
				}
				else
				{
					byte[] tmp_iv = Encoding.ASCII.GetBytes(subprops.iv);
					Array.Copy(tmp_iv, iv, tmp_iv.Length);
				}
			}
			/* iv end */

			/* key */
			if (subprops.key == null || subprops.key.Length == 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix +
					Lang.Tools.AesRes.Error_NoKey);
				return 1;
			}

			if ((!subprops.hex_key && subprops.key.Length > key.Length) ||
				(subprops.hex_key && subprops.key.Length != key.Length * 2))
			{
				if (subprops.hex_key)
					Console.Error.Write(
						Lang.Resource.Main_Error_Prefix +
						Lang.Tools.AesRes.Error_InvalidKeyLenHex);
				else
					Console.Error.Write(
						Lang.Resource.Main_Error_Prefix +
						Lang.Tools.AesRes.Error_LongKeyLen, key.Length);

				return 1;
			}

			if (subprops.hex_key)
			{
				if (!Utils.StrToByteArray(ref subprops.key, out key))
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix +
						Lang.Tools.AesRes.Error_InvalidKeyHex);
					if (iv != null)
						Console.Error.WriteLine("(char: \"{0}\")", subprops.key);

					return 1;
				}
			}
			else
			{
				byte[] tmp_key = Encoding.ASCII.GetBytes(subprops.key);
				Array.Copy(tmp_key, key, tmp_key.Length);
			}
			/* key end */
			/*
			 * check/build iv and key end
			 */

			/* check offset/length */
			if (subprops.offset > fw.inFInfo.Length)
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.AesRes.Warning_LargeOffset);
			else
				offset = subprops.offset;

			if (subprops.len != null &&						// something is specified for len
				(!Utils.StrToLong(subprops.len, out len, NumberStyles.None) ||// fail to convert (invalid chars for num)
				len <= 0 ||							// equal or smaller than 0
				len > fw.inFInfo.Length - offset))				// larger than valid length
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.AesRes.Warning_InvalidLength);
				subprops.len = null;
			}

			if (subprops.len != null ?
				len % 16 != 0 :				// if "length" specified
				(fw.inFInfo.Length - offset) % 16 != 0)	// no length specified
			{
				if (subprops.decrypt)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix +
						Lang.Tools.AesRes.Error_InvalidDecLen);
					return 1;
				}
				else
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Warning_Prefix +
						Lang.Tools.AesRes.Warning_ShortEncLen);
				}
			}
			/* check offset/length end */

			if (!props.quiet)
				PrintInfo(subprops, key, iv,
					subprops.len != null ? len : fw.inFInfo.Length - offset);

			AesManaged aes = new AesManaged
			{
				KeySize = keylen,
				IV = iv,
				Key = key,
				Mode = CipherMode.CBC,
				Padding = PaddingMode.Zeros
			};

			ICryptoTransform endec = subprops.decrypt ?
				aes.CreateDecryptor(aes.Key, aes.IV) :
				aes.CreateEncryptor(aes.Key, aes.IV);

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Write))
				using (fw.outFs = new FileStream(props.outFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
				using (Cs = new CryptoStream(fw.outFs, endec, CryptoStreamMode.Write))
				{
					fw.inFs.Seek(offset, SeekOrigin.Begin);

					byte[] buf = new byte[0x10000];
					int readlen;
					while ((readlen = fw.inFs.Read(buf, 0, buf.Length)) > 0)
					{
						if (subprops.len == null)
							Cs.Write(buf, 0, readlen);
						else if (len > readlen)
						{
							len -= readlen;
							Cs.Write(buf, 0, readlen);
						}
						else
						{
							Cs.Write(buf, 0, (int)len);
							break;
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			return 0;
		}
	}
}
