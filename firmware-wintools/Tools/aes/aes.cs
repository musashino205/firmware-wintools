using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace firmware_wintools.Tools
{
	class Aes
	{
		public struct Properties
		{
			public string iv;
			public string key;
			public bool hex_iv;
			public bool hex_key;
			public int keylen;
			public int offset;
			public int len;
			public bool decrypt;
		}

		private void PrintHelp()
		{
			Console.WriteLine(Lang.Tools.AesRes.Help_Usage +
				Lang.Tools.AesRes.FuncDesc +
				Environment.NewLine + Environment.NewLine +
				Lang.Tools.AesRes.Help_Options +
				Lang.Resource.Help_Options_i +
				Lang.Resource.Help_Options_o +
				Lang.Tools.AesRes.Help_Options_d +
				Lang.Tools.AesRes.Help_Options_k +
				Lang.Tools.AesRes.Help_Options_K2 +
				Lang.Tools.AesRes.Help_Options_v +
				Lang.Tools.AesRes.Help_Options_V2 +
				Lang.Tools.AesRes.Help_Options_l +
				Lang.Tools.AesRes.Help_Options_O2 +
				Lang.Tools.AesRes.Help_Options_s);
		}

		private void PrintInfo(Properties subprops, byte[] key, byte[] iv, int filelen)
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
				subprops.len > 0 ?
					subprops.len :
					filelen);
			Console.WriteLine(					// offset
				Lang.Tools.AesRes.Info_offset,
				subprops.offset);
		}

		public int Do_Aes(string[] args, Program.Properties props)
		{
			byte[] iv;
			byte[] key;
			int keylen;
			int offset = 0;
			int len = -1;	// not specified
			Properties subprops = new Properties()
			{
				keylen = 256,
				len = -1
			};
			CryptoStream Cs;

			ToolsArgMap argMap = new ToolsArgMap();
			argMap.Init_args_Aes(args, ref subprops);

			if (props.help)
			{
				PrintHelp();
				return 0;
			}

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
				if (!subprops.hex_iv && subprops.iv.Length > iv.Length)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix +
						Lang.Tools.AesRes.Error_LongIVLen);
					return 1;
				}
				else if (subprops.hex_iv)
				{
					if (subprops.iv.Length % 2 != 0)
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.AesRes.Error_InvalidIVLenHex);
						return 1;
					}

					if (subprops.iv.Length > iv.Length * 2)
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.AesRes.Error_LongIVLenHex);
					}
				}

				if (subprops.hex_iv)
				{
					for (int i = 0; i < (subprops.iv.Length / 2); i++)
						iv[i] = Convert.ToByte(subprops.iv.Substring(i * 2, 2), 16);
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

			if (!subprops.hex_key && subprops.key.Length > key.Length)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + 
					Lang.Tools.AesRes.Error_LongKeyLen, key.Length);
				return 1;
			}
			else if (subprops.hex_key)
			{
				if (subprops.key.Length % 2 != 0)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix +
						Lang.Tools.AesRes.Error_InvalidKeyLenHex);
					return 1;
				}

				if (subprops.key.Length > key.Length * 2)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix +
						Lang.Tools.AesRes.Error_LongKeyLenHex, key.Length, key.Length * 2);
					return 1;
				}
			}

			if (subprops.hex_key)
			{
				for (int i = 0; i < (subprops.key.Length / 2); i++)
					key[i] = Convert.ToByte(subprops.key.Substring(i * 2, 2), 16);
			}
			else
			{
				byte[] tmp_key = Encoding.ASCII.GetBytes(subprops.key);
				Array.Copy(tmp_key, key, tmp_key.Length);

				//if (tmp_key.Length < keylen / 8)
				//	Console.Error.WriteLine("specified key is too short, padded by '0'");
			}
			/* key end */
			/*
			 * check/build iv and key end
			 */

			FileStream inFs;
			FileStream outFs;
			FileMode outMode =
				File.Exists(props.outFile) ? FileMode.Truncate : FileMode.Create;
			try
			{
				inFs = new FileStream(props.inFile, FileMode.Open, FileAccess.Read, FileShare.Write);
				outFs = new FileStream(props.outFile, outMode, FileAccess.Write, FileShare.None);
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			/* check file size for int */
			if (inFs.Length > 0x7FFFFFFFL)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix +
					Lang.Tools.AesRes.Error_LargeFile, 0x7FFFFFFFL);
				return 1;
			}

			/* check offset/length */
			if (subprops.offset > inFs.Length)
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.AesRes.Warning_LargeOffset);
			else
				offset = subprops.offset;

			if (subprops.len == 0 || subprops.len > inFs.Length - offset)
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.AesRes.Warning_InvalidLength);
			else
				len = subprops.len;
			/* check offset/length end */

			PrintInfo(subprops, key, iv, (int)inFs.Length);

			byte[] inData = new byte[len != -1 ? len : inFs.Length - offset];
			inFs.Seek(offset, SeekOrigin.Begin);
			inFs.Read(inData, 0, len != -1 ? len : (int)inFs.Length - offset);

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
			Cs = new CryptoStream(outFs, endec, CryptoStreamMode.Write);

			Cs.Write(inData, 0, inData.Length);

			inFs.Close();
			outFs.Close();

			return 0;
		}
	}
}
