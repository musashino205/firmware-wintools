using System;
using System.IO;
using System.Text;

namespace firmware_wintools.Tools
{
	class Buffalo_Enc
	{
		const string DEFAULT_KEY = "Buffalo";
		const string DEFAULT_MAGIC = "start";

		public struct Properties
		{
			public string crypt_key;
			public string magic;
			public bool islong;
			public byte seed;
			public string product;
			public string version;
			public bool isde;
			public int offset;
			public int size;
			public uint cksum;
		}

		private void PrintHelp()
		{
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Help_Usage +
				Lang.Tools.BuffaloEncRes.FuncDesc +
				Environment.NewLine + Environment.NewLine +
				Lang.Tools.BuffaloEncRes.Help_Options +
				Lang.Resource.Help_Options_i +
				Lang.Resource.Help_Options_o +
				Lang.Tools.BuffaloEncRes.Help_Options_d +
				Lang.Tools.BuffaloEncRes.Help_Options_l +
				Lang.Tools.BuffaloEncRes.Help_Options_k +
				Lang.Tools.BuffaloEncRes.Help_Options_m +
				Lang.Tools.BuffaloEncRes.Help_Options_p +
				Lang.Tools.BuffaloEncRes.Help_Options_v +
				Lang.Tools.BuffaloEncRes.Help_Options_o2 +
				Lang.Tools.BuffaloEncRes.help_Options_S,
				DEFAULT_KEY, DEFAULT_MAGIC);
		}

		private void PrintInfo(Properties subprops, bool isdbg)
		{
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info, subprops.isde ?
				Lang.Tools.BuffaloEncRes.Info_Decrypt : Lang.Tools.BuffaloEncRes.Info_Encrypt);
			if (isdbg)
			{
				Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Longstate, subprops.islong);
				Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Key, subprops.crypt_key);
			}
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Magic, subprops.magic);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Seed, subprops.seed);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Product, subprops.product);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Version, subprops.version);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_DataLen, subprops.size);
			Console.WriteLine(Lang.Tools.BuffaloEncRes.Info_Cksum, subprops.cksum);
		}

		private int CheckParams(Properties subprops)
		{
			if (subprops.crypt_key == null || subprops.crypt_key.Length == 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_NoKey);
				return 1;
			}
			else if (subprops.crypt_key.Length > Buffalo_Lib.BCRYPT_MAX_KEYLEN)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_LongKey,
					subprops.crypt_key);
				return 1;
			}

			if (subprops.magic.Length != Buffalo_Lib.ENC_MAGIC_LEN - 1)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_InvalidMagicLen,
					Buffalo_Lib.ENC_MAGIC_LEN - 1);
				return 1;
			}

			if (!subprops.isde)
			{
				if (subprops.product == null)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_NoProduct);
					return 1;
				}
				else if (subprops.product.Length > Buffalo_Lib.ENC_PRODUCT_LEN - 1)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_LongProduct,
						subprops.product);
					return 1;
				}

				if (subprops.version == null)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_NoVersion);
					return 1;
				}
				else if (subprops.version.Length > Buffalo_Lib.ENC_VERSION_LEN - 1)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_LongVersion,
						subprops.version);
					return 1;
				}
			}

			return 0;
		}

		private void CloseStream(ref FileStream inFs, ref FileStream outFs)
		{
			inFs.Close();
			outFs.Close();
		}

		private int Encrypt(Program.Properties props, Properties subprops)
		{
			long src_len, tail_dst = 0, tail_len = 0, tail_src;
			long totlen = 0;
			byte[] buf;
			uint hdrlen;
			Buffalo_Lib.Enc_Param ep = new Buffalo_Lib.Enc_Param()
			{
				key = new byte[subprops.crypt_key.Length + 1],
				magic = new byte[subprops.magic.Length + 1],
				seed = subprops.seed,
				product = new byte[subprops.product.Length + 1],
				version = new byte[subprops.version.Length + 1],
				longstate = subprops.islong,
			};

			/* key, magic, product, versionは末尾 '\0' で終端 */
			Array.Copy(Encoding.ASCII.GetBytes(subprops.crypt_key), 0,
				ep.key, 0, subprops.crypt_key.Length);
			Array.Copy(Encoding.ASCII.GetBytes(subprops.magic), 0,
				ep.magic, 0, subprops.magic.Length);
			Array.Copy(Encoding.ASCII.GetBytes(subprops.product), 0,
				ep.product, 0, subprops.product.Length);
			Array.Copy(Encoding.ASCII.GetBytes(subprops.version), 0,
				ep.version, 0, subprops.version.Length);

			FileStream inFs;
			FileStream outFs;
			FileMode outFMode =
				File.Exists(props.outFile) ? FileMode.Truncate : FileMode.Create;
			try
			{
				inFs = new FileStream(props.inFile, FileMode.Open, FileAccess.Read, FileShare.Read);
				outFs = new FileStream(props.outFile, outFMode, FileAccess.Write, FileShare.None);
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			src_len = inFs.Length;

			Buffalo_Lib bufLib = new Buffalo_Lib();
			if (subprops.size > 0)
			{
				tail_dst = (long)bufLib.Enc_Compute_BufLen(ref ep.product, ref ep.version, (ulong)subprops.size);
				tail_len = src_len - subprops.size;
				totlen = tail_dst + tail_len;
			}
			else
				totlen = (long)bufLib.Enc_Compute_BufLen(ref ep.product, ref ep.version, (ulong)src_len);

			buf = new byte[totlen];

			hdrlen = (uint)bufLib.Enc_Compute_HeaderLen(ref ep.product, ref ep.version);

			inFs.Read(buf, (int)hdrlen, (int)src_len);

			if (subprops.size > 0)
			{
				tail_src = hdrlen + subprops.size;
				Array.Copy(buf, tail_src, buf, tail_dst, tail_len);
				Array.Clear(buf, (int)tail_src, (int)(tail_dst - tail_src));
				src_len = subprops.size;
			}

			ep.cksum = bufLib.Buffalo_Csum((uint)src_len, ref buf, hdrlen, (ulong)src_len);
			ep.datalen = (uint)src_len;

			subprops.cksum = ep.cksum;
			subprops.size = (int)ep.datalen;

			PrintInfo(subprops, props.debug);
			if (bufLib.Encrypt_Buf(ref ep, ref buf, hdrlen) != 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_FailEncrypt);
				CloseStream(ref inFs, ref outFs);
				return 1;
			}

			outFs.Write(buf, 0, (int)totlen);

			CloseStream(ref inFs, ref outFs);

			return 0;
		}

		private int Decrypt(Program.Properties props, Properties subprops)
		{
			long src_len;
			byte[] buf;
			Buffalo_Lib.Enc_Param ep = new Buffalo_Lib.Enc_Param();

			FileStream inFs;
			FileStream outFs;
			FileMode outFMode =
				File.Exists(props.outFile) ? FileMode.Truncate : FileMode.Create;
			try
			{
				inFs = new FileStream(props.inFile, FileMode.Open, FileAccess.Read, FileShare.Read);
				outFs = new FileStream(props.outFile, outFMode, FileAccess.Write, FileShare.None);
			} catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			src_len = inFs.Length;

			if (!inFs.CanSeek || src_len < subprops.offset)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_LargeOffset,
					subprops.offset);
				CloseStream(ref inFs, ref outFs);
				return 1;
			}

			inFs.Seek(subprops.offset, SeekOrigin.Begin);
			buf = new byte[src_len - subprops.offset];

			inFs.Read(buf, 0, Convert.ToInt32(src_len) - subprops.offset);

			ep.magic = new byte[Buffalo_Lib.ENC_MAGIC_LEN];
			ep.product = new byte[Buffalo_Lib.ENC_PRODUCT_LEN];
			ep.version = new byte[Buffalo_Lib.ENC_VERSION_LEN];
			ep.key = Encoding.ASCII.GetBytes(subprops.crypt_key);
			ep.longstate = subprops.islong;
		
			Buffalo_Lib bufLib = new Buffalo_Lib();
			if (bufLib.Decrypt_Buf(ref ep, ref buf, buf.LongLength) != 0){
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.BuffaloEncRes.Error_FailDecrypt);
				CloseStream(ref inFs, ref outFs);
				return 1;
			}

			subprops.magic = Encoding.ASCII.GetString(ep.magic).TrimEnd('\0');
			subprops.seed = ep.seed;
			subprops.product = Encoding.ASCII.GetString(ep.product).TrimEnd('\0');
			subprops.version = Encoding.ASCII.GetString(ep.version).TrimEnd('\0');
			subprops.size = Convert.ToInt32(ep.datalen);
			subprops.cksum = ep.cksum;

			PrintInfo(subprops, props.debug);

			outFs.Write(buf, 0, Convert.ToInt32(ep.datalen));

			CloseStream(ref inFs, ref outFs);

			return 0;
		}

		public int Do_BuffaloEnc(string[] args, Program.Properties props)
		{
			int ret = 0;
			Properties subprops = new Properties();

			subprops.crypt_key = DEFAULT_KEY;
			subprops.magic = DEFAULT_MAGIC;
			subprops.seed = 0x4F;   // Char: O

			ToolsArgMap argMap = new ToolsArgMap();
			argMap.Init_args_BuffaloEnc(args, ref subprops);

			if (props.help)
			{
				PrintHelp();
				return 0;
			}

			if (CheckParams(subprops) != 0)
				return 1;

			ret = subprops.isde ?
				Decrypt(props, subprops) : Encrypt(props, subprops);

			return ret;
		}
	}
}
