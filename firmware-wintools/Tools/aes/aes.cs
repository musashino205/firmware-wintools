using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace firmware_wintools.Tools
{
	internal class Aes : Tool
	{
		/* ツール情報　*/
		public override string name { get => "aes"; }
		public override string desc { get => Lang.Tools.AesRes.FuncDesc; }
		public override string descFmt { get => Lang.Tools.AesRes.Main_FuncDesc_Fmt; }
		public override string resName => "AesRes";

		private bool Decrypt = false;
		private string Key = "";
		private string IV = "";
		private string PSK = "";
		private bool HexKey = false;
		private bool HexIV = false;
		private bool ShortKey = false;
		private bool UsePSK = false;
		private long Length = -1;
		private long Offset = 0;

		internal override List<Param> ParamList => new List<Param>() {
			new Param() { PChar = 'd', PType = Param.PTYPE.BOOL, SetField = "Decrypt", HelpKey = "Help_Options_d" },
			new Param() { PChar = 'k', PType = Param.PTYPE.STR, SetField = "Key", HelpKey = "Help_Options_k" },
			new Param() { PChar = 'K', PType = Param.PTYPE.STR, SetField = "Key", SetBool = "HexKey", HelpKey = "Help_Options_K2" },
			new Param() { PChar = 'v', PType = Param.PTYPE.STR, SetField = "IV", HelpKey = "Help_Options_v" },
			new Param() { PChar = 'V', PType = Param.PTYPE.STR, SetField = "IV", SetBool = "HexIV", HelpKey = "Help_Options_V2" },
			new Param() { PChar = 'p', PType = Param.PTYPE.STR, SetField = "PSK", SetBool = "UsePSK", HelpKey = "Help_Options_p" },
			new Param() { PChar = 'l', PType = Param.PTYPE.LONG, SetField = "Length", HelpKey = "Help_Options_l" },
			new Param() { PChar = 'O', PType = Param.PTYPE.LONG, SetField = "Offset", HelpKey = "Help_Options_O2" },
			new Param() { PChar = 's', PType = Param.PTYPE.BOOL, SetField = "ShortKey", HelpKey = "Help_Options_s" },
		};

		private void PrintInfo(byte[] key, byte[] iv, byte[] salt)
		{
			Console.WriteLine(Lang.Tools.AesRes.Info,
				Decrypt ?
					Lang.Tools.AesRes.Info_Decrypt :
					Lang.Tools.AesRes.Info_Encrypt);
			Console.WriteLine(Lang.Tools.AesRes.Info_mode,
				"AES-" + (ShortKey ? 128 : 256) + "-CBC");
			if (UsePSK)
				Console.WriteLine(Lang.Tools.AesRes.Info_salt,
					BitConverter.ToString(salt).Replace("-", ""));
			if (HexKey || UsePSK)
				Console.WriteLine(Lang.Tools.AesRes.Info_key2,
					BitConverter.ToString(key).Replace("-", ""));
			else
				Console.WriteLine(Lang.Tools.AesRes.Info_key,
					Key,
					BitConverter.ToString(key).Replace("-", ""));
			if (HexIV || UsePSK || IV.Length == 0)
				Console.WriteLine(Lang.Tools.AesRes.Info_iv2,
					BitConverter.ToString(iv).Replace("-", ""));
			else
				Console.WriteLine(Lang.Tools.AesRes.Info_iv,
					IV,
					BitConverter.ToString(iv).Replace("-", ""));
			Console.WriteLine(Lang.Tools.AesRes.Info_len, Length);
			Console.WriteLine(Lang.Tools.AesRes.Info_offset,
				Offset - (UsePSK && Decrypt ? 0x10 : 0));
		}

		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			byte[] iv;
			byte[] key;
			byte[] salt = null;
			int ret;
			CryptoStream Cs;
			Firmware fw = new Firmware();

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return ret;

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			fw.inFInfo = new FileInfo(props.inFile);
			fw.outFile = props.outFile;

			iv = new byte[16];
			key = new byte[(ShortKey ? 128 : 256) / 8];	// 128または256bit

			if (!UsePSK)
			{
				/*
				 * check/build iv and key
				 */
				/* iv */
				if (IV.Length == 0) // if iv is not specified
				{
					/* use default array of iv (filled by '0') */
					Console.Error.WriteLine(
						Lang.Resource.Main_Warning_Prefix +
						Lang.Tools.AesRes.Warning_NoIV);
				}
				else    // if iv is specified
				{
					if ((!HexIV && IV.Length > iv.Length) ||
						(HexIV && IV.Length != iv.Length * 2))
					{
						Console.Error.Write(
							Lang.Resource.Main_Error_Prefix +
							(HexIV ?
								Lang.Tools.AesRes.Error_InvalidIVLenHex :
								Lang.Tools.AesRes.Error_LongIVLen));

						return 1;
					}

					if (HexIV)
					{
						if (!Utils.StrToByteArray(ref IV, out iv))
						{
							Console.Error.WriteLine(
								Lang.Resource.Main_Error_Prefix +
								Lang.Tools.AesRes.Error_InvalidIVHex);
							if (iv != null)
								Console.Error.WriteLine("(char: \"{0}\")", IV);

							return 1;
						}
					}
					else
					{
						byte[] tmp_iv = Encoding.ASCII.GetBytes(IV);
						Array.Copy(tmp_iv, iv, tmp_iv.Length);
					}
				}
				/* iv end */

				/* key */
				if (HexKey)
				{
					if (Key.Length != key.Length * 2)
					{
						Console.Error.Write(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.AesRes.Error_InvalidKeyLenHex);

						return 1;
					}

					if (!Utils.StrToByteArray(ref Key, out key))
					{
						Console.Error.WriteLine(
							Lang.Resource.Main_Error_Prefix +
							Lang.Tools.AesRes.Error_InvalidKeyHex);
						if (key != null)
							Console.Error.WriteLine("(char: \"{0}\")", Key);

						return 1;
					}
				}
				else
				{
					if (Key.Length > key.Length)
					{
						Console.Error.Write(
						Lang.Resource.Main_Error_Prefix +
						Lang.Tools.AesRes.Error_LongKeyLen, key.Length);

						return 1;
					}

					byte[] tmp_key = Encoding.ASCII.GetBytes(Key);
					Array.Copy(tmp_key, key, tmp_key.Length);
				}
				/* key end */
				/*
				 * check/build iv and key end
				 */
			}
			/*
			 * 事前共有鍵(PSK)
			 * 構造参考: https://qiita.com/angel_p_57/items/bc50c5cfbb0276e07707
			 * ロジック参考: https://qiita.com/c-yan/items/1e8f66f6b1019aad56bd
			 */
			else
			{
				salt = new byte[8];

				if (!Decrypt) /* 暗号化時ランダム生成 */
					using (var csp = new RNGCryptoServiceProvider())
						csp.GetBytes(salt);
				else /* 復号時 "Salted__" 後8byte読み取り */
				{
					try
					{
						using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
								FileAccess.Read, FileShare.Write))
						{
							fw.inFs.Seek(Offset, SeekOrigin.Begin);

							byte[] buf = new byte[16];
							int readlen;

							readlen = fw.inFs.Read(buf, 0, buf.Length);
							/* 16byte読めなかったか先頭8byteが "Salted__" ではない */
							if (readlen != buf.Length ||
							    !Encoding.ASCII.GetString(buf, 0, 8).Equals("Salted__")) {
								Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
										Lang.Tools.AesRes.Error_FailReadSalt);
								return 1;
							}

							Buffer.BlockCopy(buf, 8, salt, 0, salt.Length);
						}

					}
					catch (IOException e)
					{
						Console.Error.WriteLine(e.Message);
						return 1;
					}

					/* 暗号化データは "Salted__"(0x8) + salt(0x8) の後 */
					Offset += 0x10;
				}

				/* Key, IV算出 (MD5) */
				byte[] psksalt = new byte[PSK.Length + 8];
				byte[] tmp = new byte[psksalt.Length + 16]; /* 16: MD5ハッシュ長 */
				byte[] hash;

				using (MD5 md5 = MD5.Create())
				{
					/* psksaltセットアップ */
					Buffer.BlockCopy(Encoding.ASCII.GetBytes(PSK), 0,
							psksalt, 0, PSK.Length);
					Buffer.BlockCopy(salt, 0, psksalt, PSK.Length, salt.Length);

					hash = md5.ComputeHash(psksalt);
					Buffer.BlockCopy(hash, 0, key, 0, hash.Length);
					/* tmpセットアップ (tmp: (16bytes),PSK,Salt) */
					Buffer.BlockCopy(psksalt, 0, tmp, hash.Length, psksalt.Length);
					if (!ShortKey) /* 256bit */
					{
						/* tmp: md5(PSK,Salt),PSK,Salt */
						Buffer.BlockCopy(key, 0, tmp, 0, hash.Length);
						hash = md5.ComputeHash(tmp);
						/* key: md5(PSK,Salt),md5(md5(PSK,Salt),PSK,Salt) */
						Buffer.BlockCopy(hash, 0, key, hash.Length, hash.Length);
					}

					Buffer.BlockCopy(ShortKey ? key : hash, 0, tmp, 0, hash.Length);
					iv = md5.ComputeHash(tmp);
				}
			}

			/* check offset/length */
			if (Offset >= fw.inFInfo.Length)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.AesRes.Warning_LargeOffset);
				Offset = 0;
			}

			if (Length == -1)
				Length = fw.inFInfo.Length - Offset;

			if (Length == 0 ||
			    Length > fw.inFInfo.Length - Offset)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.AesRes.Warning_InvalidLength);
				Length = fw.inFInfo.Length - Offset;
			}

			if (Length % 16 != 0)
			{
				Console.Error.WriteLine(
					Decrypt ?
						Lang.Resource.Main_Error_Prefix +
						Lang.Tools.AesRes.Error_InvalidDecLen :
						Lang.Resource.Main_Warning_Prefix +
						Lang.Tools.AesRes.Warning_ShortEncLen);
				if (Decrypt)
					return 1;
			}
			/* check offset/length end */

			if (!props.quiet)
				PrintInfo(key, iv, salt);

			AesManaged aes = new AesManaged
			{
				KeySize = ShortKey ? 128 : 256,
				IV = iv,
				Key = key,
				Mode = CipherMode.CBC,
				Padding = PaddingMode.Zeros
			};

			ICryptoTransform endec = Decrypt ?
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
					fw.inFs.Seek(Offset, SeekOrigin.Begin);

					byte[] buf = new byte[0x10000];

					if (!Decrypt && UsePSK) {
						Buffer.BlockCopy(
							Encoding.ASCII.GetBytes("Salted__"), 0, buf, 0, 8);
						Buffer.BlockCopy(salt, 0, buf, 8, 8);

						fw.outFs.Write(buf, 0, 16);
					}

					int readlen;
					while ((readlen = fw.inFs.Read(buf, 0, buf.Length)) > 0)
					{
						if (Length > readlen)
						{
							Length -= readlen;
							Cs.Write(buf, 0, readlen);
						}
						else
						{
							Cs.Write(buf, 0, (int)Length);
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
