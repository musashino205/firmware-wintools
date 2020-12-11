using System;
using System.IO;
using System.Text;

namespace firmware_wintools.Tools
{
	class XorImage
	{
		/// <summary>
		/// xorimageの機能プロパティ
		/// </summary>
		public struct Properties
		{
			/// <summary>
			/// xorを行うデータ長
			/// </summary>
			public string len;
			/// <summary>
			/// xorを開始するデータのオフセット
			/// </summary>
			public long offset;
			/// <summary>
			/// xorに用いるpattern
			/// </summary>
			public string pattern;
			/// <summary>
			/// 指定されたパターンがhex値であるか否か
			/// </summary>
			public bool ishex;
			/// <summary>
			/// 部分書き換えモード
			/// </summary>
			public bool rewrite;
		}

		/// <summary>
		/// xorimageの機能ヘルプを表示します
		/// </summary>
		private void PrintHelp()
		{
			Console.WriteLine(Lang.Tools.XorImageRes.Help_Usage +
				Lang.Tools.XorImageRes.FuncDesc +
				Environment.NewLine);
			// 共通オプション表示
			Program.PrintCommonOption();
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				Lang.Tools.XorImageRes.Help_Options_Pattern +
				Lang.Tools.XorImageRes.Help_Options_Hex +
				Lang.Tools.XorImageRes.Help_Options_Rewrite);
		}

		/// <summary>
		/// xorimageの実行情報を表示します
		/// </summary>
		/// <param name="props"></param>
		private void PrintInfo(Properties subprops, long datalen)
		{
			Console.WriteLine(Lang.Tools.XorImageRes.Info);
			Console.WriteLine(Lang.Tools.XorImageRes.Info_Pattern, subprops.pattern);
			Console.WriteLine(Lang.Tools.XorImageRes.Info_Hex, subprops.ishex.ToString());
			Console.WriteLine(			// length
				Lang.Tools.XorImageRes.Info_len,
				datalen);
			Console.WriteLine(			// offset
				Lang.Tools.XorImageRes.Info_offset,
				subprops.offset);
			Console.WriteLine(
				Lang.Tools.XorImageRes.Info_Rewrite,
				subprops.rewrite);
		}

		/// <summary>
		/// 指定された <paramref name="props"/> 内のpatternを用いて、<paramref name="data"/>
		/// のxorを行います
		/// </summary>
		/// <param name="data">xor対象データ</param>
		/// <param name="len">xor対象データの長さ</param>
		/// <param name="props">xorimageの機能プロパティ</param>
		/// <param name="p_len">パターン長</param>
		/// <param name="p_off">パターン オフセット</param>
		/// <returns></returns>
		private int XorData(ref byte[] data, int len, in byte[] pattern, int p_len, int p_off, bool ishex)
		{
			int data_pos = 0;

			while (len-- > 0)
			{
				data[data_pos] ^= pattern[p_off];
				data_pos++;
				p_off = (p_off + 1) % (ishex ? p_len / 2 : p_len);
			}

			return p_off;
		}

		/// <summary>
		/// xorimageメイン関数
		/// <para>コマンドライン引数とメインプロパティから、xorによりファームウェアの
		/// エンコード/デコード を行います</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">Program内のメインプロパティ</param>
		/// <returns></returns>
		public int Do_XorImage(string[] args, Program.Properties props)
		{
			int read_len, write_len, p_off = 0;
			long offset = 0, len = long.MaxValue;
			byte[] pattern;
			byte[] hex_pattern = new byte[128];
			byte[] buf = new byte[4096];
			Properties subprops = new Properties
			{
				pattern = "12345678"
			};

			if (props.help)
			{
				PrintHelp();
				return 0;
			}

			ToolsArgMap argMap = new ToolsArgMap();
			argMap.Init_args_Xorimage(args, ref subprops);

			int p_len = subprops.pattern.Length;

			if (p_len == 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.XorImageRes.Error_InvalidPatternLen);
				return 1;
			}

			if (subprops.ishex)
			{
				if ((p_len / 2) > hex_pattern.Length)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.XorImageRes.Error_LongHexPattern);
					return 1;
				}

				if (p_len % 2 != 0)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.XorImageRes.Error_InvalidHexPatternLen);
					return 1;
				}
			}

			if (subprops.ishex)
			{
				pattern = new byte[p_len / 2];
				for (int i = 0; i < (p_len / 2); i++)
					pattern[i] = Convert.ToByte(subprops.pattern.Substring(i * 2, 2), 16);
			}
			else
				pattern = Encoding.ASCII.GetBytes(subprops.pattern);

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

			/* check offset/length */
			if (subprops.offset > inFs.Length)
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.XorImageRes.Warning_LargeOffset);
			else
				offset = subprops.offset;

			if (subprops.len != null &&					// something is specified for len
				(Program.StrToLong(subprops.len, out len, 0) != 0 ||	// fail to convert (invalid chars for num)
				len <= 0 ||						// equal or smaller than 0
				len > inFs.Length - offset))				// larger than valid length
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.XorImageRes.Warning_InvalidLength);
				len = long.MaxValue;
			}
			/* check offset/length end */

			if (!props.quiet)
				PrintInfo(subprops, len != long.MaxValue ? len : inFs.Length - offset);

			/* copy data of the range 0x0 to offset to outFs if rewrite mode */
			if (subprops.rewrite)
				while ((read_len = inFs.Read(buf, 0, buf.Length)) > 0)
				{
					if (inFs.Position <= offset)
					{
						outFs.Write(buf, 0, read_len);
					}
					else
					{
						outFs.Write(buf, 0, read_len - (int)(inFs.Position - offset));
						break;
					}
				}

			inFs.Seek(offset, SeekOrigin.Begin);

			while ((read_len = inFs.Read(buf, 0, buf.Length)) > 0)
			{
				write_len = read_len;

				if (len != long.MaxValue)
					if (len > read_len)
						len -= read_len;	// 読み取った長さよりも残りの対象データ長が長い場合差し引く
					else
						write_len = (int)len;	// 残りデータ長が読み取った長さ以下である場合残りデータ長を使う

				p_off = XorData(ref buf, write_len, in pattern, p_len, p_off, subprops.ishex);

				outFs.Write(buf, 0, write_len);

				/*
				 * 読み取った長さが対象データ長以下である場合breakしてXorと書き込みを終了
				 * len <= read_lenであるならばlengthが正しい数値で指定され（long.MaxValueでない）、
				 * なおかつ最後のブロックであるので、inFsから残りをoutFsへコピーするため読み取った
				 * データ長から実際に書き込んだデータ長を差し引いたサイズで現在位置からマイナス方向に
				 * Seekする
				 */
				if (len <= read_len)
				{
					inFs.Seek(-(read_len - write_len), SeekOrigin.Current);
					break;
				}
			}

			/* copy remaining data in inFs to outFs if rewrite mode */
			if (subprops.rewrite)
				inFs.CopyTo(outFs);

			inFs.Close();
			outFs.Close();

			return 0;
		}
	}
}
