using System;
using System.IO;
using System.Text;

namespace firmware_wintools.Tools
{
	class Nec_Enc
	{
		/// <summary>
		/// nec-encの機能プロパティ
		/// </summary>
		public struct Properties
		{
			/// <summary>
			/// patternのxorに用いるキー
			/// </summary>
			public string key;
		}

		/// <summary>
		/// nec-encの機能ヘルプを表示します
		/// </summary>
		private void PrintHelp()
		{
			Console.WriteLine("Usage: firmware-wintools nec-enc [OPTIONS...]\n" +
				Environment.NewLine +
				"Options:\n" +
				"  -i <file>\tinput file\n" +
				"  -o <file>\toutput file\n" +
				"  -k <key>\tuse <key> for encode/decode the firmware\n");
		}

		/// <summary>
		/// nec-encの実行情報を表示します
		/// </summary>
		/// <param name="props">nec-encの機能プロパティ</param>
		private void PrintInfo(Properties props)
		{
			Console.WriteLine("===== nec-enc mode =====");
			Console.WriteLine(" key:\t\t{0}\n", props.key);
		}

		/// <summary>
		/// keyを用いてxorによりpatternを生成します
		/// </summary>
		/// <param name="data">ベースパターン データ</param>
		/// <param name="len">ベースパータンのデータ長</param>
		/// <param name="key">ベースパターンのxorに用いるキー</param>
		/// <param name="k_len">キー長</param>
		/// <param name="k_off">キー オフセット</param>
		/// <returns></returns>
		private int XorPattern(ref byte[] data, int len, ref string key, int k_len, int k_off)
		{
			int data_pos = 0;
			int offset = k_off;
			byte[] byteKey = Encoding.UTF8.GetBytes(key);

			while (len-- > 0)
			{
				data[data_pos] ^= byteKey[offset];
				data_pos++;
				offset = (offset + 1) % k_len;
			}

			return offset;
		}

		/// <summary>
		/// patternを用いて対象データのxorを行います
		/// <para>対象データとpatternは長さが同一である必要があります</para>
		/// </summary>
		/// <param name="data">xor対象データ</param>
		/// <param name="len">xor対象データの長さ</param>
		/// <param name="pattern">xorに用いるpattern</param>
		private void XorData(ref byte[] data, int len, ref byte[] pattern)
		{
			int data_pos = 0;

			for (int i = 0; i < len; i++)
			{
				data[data_pos] ^= pattern[i];
				data_pos++;
			}
		}

		/// <summary>
		/// nec-encメイン関数
		/// <para>コマンドライン引数とメインプロパティから、多重xorを用いた
		/// NEC Aterm シリーズ用ファームウェアのエンコード/デコードを行います
		/// </para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">Program内メインプロパティ</param>
		/// <returns>実行結果</returns>
		public int Do_NecEnc(string[] args, Program.Properties props)
		{
			int max_key_len = 16;
			int pattern_len = 251;
			int read_len;
			int ptn = 1;
			int k_off = 0;
			byte[] buf_pattern = new byte[4096];
			byte[] buf = new byte[4096];
			Properties subprops = new Properties();

			ToolsArgMap argMap = new ToolsArgMap();
			argMap.Init_args_NecEnc(args, ref subprops);

			if (props.help)
			{
				PrintHelp();
				return 0;
			}

			if (subprops.key == null)
			{
				Console.WriteLine("error: \"key\" is not specified");
				return 1;
			}

			int k_len = subprops.key.Length;
			if (k_len == 0 || k_len > max_key_len)
			{
				Console.WriteLine("Key length is not in range (0, {0})", max_key_len);
				return 1;
			}

			PrintInfo(subprops);

			if (!File.Exists(props.inFile))
			{
				Console.WriteLine("cannot open input file (not found)");
				return 1;
			}

			FileStream inFs = null;
			FileStream outFs = null;
			FileStream patFs = null;
			FileStream xpatFs = null;

			try
			{
				inFs = new FileStream(props.inFile, FileMode.Open, FileAccess.Read);
				outFs = new FileStream(props.outFile, FileMode.OpenOrCreate, FileAccess.Write);

				if (props.debug)
				{
					patFs = new FileStream(@"pattern.bin", FileMode.OpenOrCreate, FileAccess.Write);
					xpatFs = new FileStream(@"pattern.xor", FileMode.OpenOrCreate, FileAccess.Write);
				}
			}
			catch (IOException i)
			{
				Console.WriteLine(i.Message);
				return 1;
			}

			while ((read_len = inFs.Read(buf, 0, buf.Length)) > 0)
			{
				for (int i = 0; i < read_len; i++)
				{
					buf_pattern[i] = Convert.ToByte(ptn);
					ptn++;

					if (ptn > pattern_len)
						ptn = 1;
				}

				if (props.debug)
					patFs.Write(buf_pattern, 0, read_len);

				k_off = XorPattern(ref buf_pattern, read_len, ref subprops.key, k_len, k_off);
				if (props.debug)
					xpatFs.Write(buf_pattern, 0, read_len);

				XorData(ref buf, read_len, ref buf_pattern);

				outFs.Write(buf, 0, read_len);
			}

			inFs.Close();
			outFs.Close();
			if (props.debug)
			{
				patFs.Close();
				xpatFs.Close();
			}

			return 0;
		}
	}
}
