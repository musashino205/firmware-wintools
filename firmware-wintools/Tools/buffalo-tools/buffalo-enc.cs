using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace firmware_wintools.Tools
{
	class Buffalo_Enc
	{
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
			Console.WriteLine("Usage: firmware-wintools nec-enc [OPTIONS...]\n" +
				Environment.NewLine +
				"Options:\n" +
				"  -i <file>\tinput file\n" +
				"  -o <file>\toutput file\n" +
				"  -d\t\tuse decrypt mode instead of encrypt\n" +
				"  -l\t\tuse longstate {en,de}cryption method\n" +
				"  -k <key>\tuse <key> for encryption (default: \"Buffalo\")\n" +
				"  -m <magic>\tuse <magic>\n" +
				"  -p <product>\tuse <product> name for encryption\n" +
				"  -v <version>\tuse <version> for encryption\n" +
				"  -O <offset>\toffset of encrypted data in file (for decryption)\n" +
				"  -S <size>\tsize of unencrypted data in file (for encryption)\n");
		}

		private void PrintInfo(Properties props, bool isdbg)
		{
			Console.WriteLine("===== buffalo-enc mode =====");
			if (isdbg)
			{
				Console.WriteLine(" decrypt mode\t: {0}", props.isde);
				Console.WriteLine(" longstate\t: {0}", props.islong);
				Console.WriteLine(" key\t\t: '{0}'", props.crypt_key);
			}
			Console.WriteLine(" Magic:\t\t: '{0}'", props.magic);
			Console.WriteLine(" Seed\t\t: 0x{0,2:X}", props.seed);
			Console.WriteLine(" Product\t: '{0}'", props.product);
			Console.WriteLine(" Version\t: '{0}'", props.version);
			Console.WriteLine(" Data len\t: {0}", props.size);
			Console.WriteLine(" Checksum\t: 0x{0:x}", props.cksum);
		}

		private int CheckParams(Properties props)
		{
			if (props.crypt_key == null)
			{
				Console.Error.WriteLine("error: no key specified");
				return 1;
			}
			else if (props.crypt_key.Length > Buffalo_Lib.BCRYPT_MAX_KEYLEN)
			{
				Console.Error.WriteLine("error: key \"{0}\" is too long", props.crypt_key);
				return 1;
			}

			if (props.magic.Length != Buffalo_Lib.ENC_MAGIC_LEN - 1)
			{
				Console.Error.WriteLine("error: length of magic must be {0}",
					Buffalo_Lib.ENC_MAGIC_LEN - 1);
				return 1;
			}

			if (!props.isde)
			{
				if (props.product == null)
				{
					Console.Error.WriteLine("error: no product specified");
					return 1;
				}
				else if (props.product.Length > Buffalo_Lib.ENC_PRODUCT_LEN - 1)
				{
					Console.Error.WriteLine("error: specified product name {0} is too long",
						props.product);
					return 1;
				}

				if (props.version == null)
				{
					Console.Error.WriteLine("error: no version specified");
					return 1;
				}
				else if (props.version.Length > Buffalo_Lib.ENC_VERSION_LEN - 1)
				{
					Console.Error.WriteLine("error: specified version {0} is too long",
						props.version);
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
				Console.Error.WriteLine("error: failed to encrypt");
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
				Console.Error.WriteLine("error: offset is larger than input file size");
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
				Console.Error.WriteLine("error: failed to decrypt");
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

			subprops.crypt_key = "Buffalo";
			subprops.magic = "start";
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
