using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace firmware_wintools.Tools
{
	internal partial class XorImage : Tool
	{
		/* ツール情報　*/
		public override string name { get => "xorimage"; }
		public override string desc { get => Lang.Tools.XorImageRes.FuncDesc; }
		public override string descFmt { get => Lang.Tools.XorImageRes.Main_FuncDesc_Fmt; }


		/// <summary>
		/// xorを行うデータ長
		/// </summary>
		private string len_s = null;
		/// <summary>
		/// xorを開始するデータのオフセット
		/// </summary>
		private long offset = 0;
		/// <summary>
		/// xorに用いるpattern
		/// </summary>
		private string pattern = "12345678";
		/// <summary>
		/// 指定されたパターンがhex値であるか否か
		/// </summary>
		private bool ishex = false;
		/// <summary>
		/// 部分書き換えモード
		/// </summary>
		private bool rewrite = false;
		/// <summary>
		/// pattern用バイナリファイル
		/// </summary>
		private string binPattern = null;

		/// <summary>
		/// xorimageの機能ヘルプを表示します
		/// </summary>
		public static void PrintHelp(int arg_idx)
		{
			Console.WriteLine(Lang.Tools.XorImageRes.Help_Usage +
				Lang.Tools.XorImageRes.FuncDesc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");	// 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption();
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				Lang.Tools.XorImageRes.Help_Options_Pattern +
				Lang.Tools.XorImageRes.Help_Options_PatternBin +
				Lang.Tools.XorImageRes.Help_Options_Hex +
				Lang.Tools.XorImageRes.Help_Options_Length +
				Lang.Tools.XorImageRes.Help_Options_Offset +
				Lang.Tools.XorImageRes.Help_Options_Rewrite);
		}

		/// <summary>
		/// xorimageの実行情報を表示します
		/// </summary>
		/// <param name="props"></param>
		private void PrintInfo(long datalen, in byte[] ptnAry)
		{
			Console.WriteLine(Lang.Tools.XorImageRes.Info);
			Console.WriteLine(Lang.Tools.XorImageRes.Info_Pattern,
						binPattern != null ?
							BitConverter.ToString(ptnAry).Replace("-", "") :
							pattern);
			Console.WriteLine(Lang.Tools.XorImageRes.Info_Hex, ishex.ToString());
			Console.WriteLine(Lang.Tools.XorImageRes.Info_len, datalen);
			Console.WriteLine(Lang.Tools.XorImageRes.Info_offset, offset);
			Console.WriteLine(Lang.Tools.XorImageRes.Info_Rewrite, rewrite);
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
		private static int
		XorData(ref byte[] data, int len, in byte[] pattern, int p_len, int p_off)
		{
			int data_pos = 0;

			while (len-- > 0)
			{
				data[data_pos] ^= pattern[p_off];
				data_pos++;
				p_off = (p_off + 1) % p_len;
			}

			return p_off;
		}

		private int
		SetupPattern(out byte[] ptnAry, out int p_len)
		{
			FileInfo ptnBinInfo = null;
			int hexPtn_maxLen = 0x100;

			ptnAry = null;
			p_len = 0;

			/* パターンバイナリ */
			if (binPattern != null)
			{
				/*
				 * - binPatternが空文字
				 * - ファイルが存在しない
				 * - ファイルサイズが 0x100 (256 bytes) 超
				 */
				if (binPattern.Length == 0 ||
				    !File.Exists(binPattern) ||
				    (ptnBinInfo = new FileInfo(binPattern)).Length > hexPtn_maxLen)
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
								Lang.Tools.XorImageRes.Error_InvalidPatternBin);
					if (ptnBinInfo != null)
						Console.Error.WriteLine(Lang.Tools.XorImageRes.Error_PatternBinMaxLen,
								hexPtn_maxLen);
					return 1;
				}

				ptnAry = new byte[ptnBinInfo.Length];
				try
				{
					using (FileStream ptnFs = new FileStream(binPattern, FileMode.Open,
												FileAccess.Read, FileShare.Read))
					{
						ptnFs.Read(ptnAry, 0, (int)ptnFs.Length);
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
									e.Message);
					return 1;
				}

				ishex = true;
				p_len = ptnAry.Length;
				return 0;
			}

			/* テキストパターン */
			p_len = pattern.Length;
			if (p_len == 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.XorImageRes.Error_InvalidPatternLen);
				return 1;
			}

			if (ishex)
			{
				if ((p_len / 2) > hexPtn_maxLen)
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

				p_len /= 2;
				ptnAry = new byte[p_len];
				for (int i = 0; i < p_len; i++)
					ptnAry[i] = Convert.ToByte(pattern.Substring(i * 2, 2), 16);
			}
			else
				ptnAry = Encoding.ASCII.GetBytes(pattern);

			return 0;
		}

		/// <summary>
		/// xorimageメイン関数
		/// <para>コマンドライン引数とメインプロパティから、xorによりファームウェアの
		/// エンコード/デコード を行います</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">Program内のメインプロパティ</param>
		/// <returns></returns>
		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			int read_len, write_len, p_len, p_off = 0;
			long len = long.MaxValue;
			byte[] ptnAry;
			Firmware fw = new Firmware()
			{
				data = new byte[4096]
			};

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			Init_args(args, arg_idx);

			if (SetupPattern(out ptnAry, out p_len) > 0)
				return 1;

			fw.inFInfo = new FileInfo(props.inFile);

			/* check offset/length */
			if (offset > fw.inFInfo.Length)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.XorImageRes.Warning_LargeOffset);
				offset = 0;
			}

			/*
			 * - len_sに何かが指定されている
			 * - 文字列->long変換に失敗したか
			 * - lenが0以下であるか
			 * - lenが入力ファイルの長さからオフセットを引いたものより大きいか
			 */
			if (len_s != null &&
			    (!Utils.StrToLong(len_s, out len, NumberStyles.None) ||
			    len <= 0 ||
			    len > fw.inFInfo.Length - offset))
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.XorImageRes.Warning_InvalidLength);
				len = long.MaxValue;
			}
			/* check offset/length end */

			if (!props.quiet)
				PrintInfo(len != long.MaxValue ? len : fw.inFInfo.Length - offset, in ptnAry);

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				using (fw.outFs = new FileStream(props.outFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
				{
					if (rewrite && offset > 0)
					{
						fw.inFs.CopyTo(fw.outFs);
						/* outFs.Lengthをoffsetまで切り詰める */
						fw.outFs.SetLength(offset);
					}

					/* inFs.Positionをoffsetに移動 */
					fw.inFs.Seek(offset, SeekOrigin.Begin);

					while ((read_len = fw.inFs.Read(fw.data, 0, fw.data.Length)) > 0)
					{
						write_len = read_len;

						if (len != long.MaxValue)
							/* 読み取った長さよりも残りの対象データ長が長い場合 */
							if (len > read_len)
								len -= read_len;
							/* 残りデータ長が読み取った長さ以下である場合 */
							else
								write_len = (int)len;

						p_off = XorData(ref fw.data, write_len, in ptnAry, p_len, p_off);

						fw.outFs.Write(fw.data, 0, write_len);

						/*
						 * 読み取った長さが残り対象データ長以上である場合breakしてXorと書き込みを終了
						 * len <= read_lenであるならばlengthが正しい数値で指定され（long.MaxValueでない）、
						 * なおかつ最後のブロックであるので、inFsから残りをoutFsへコピーするため読み取った
						 * データ長から実際に書き込んだデータ長を差し引いたサイズで現在位置からマイナス方向に
						 * Seekする
						 */
						if (len <= read_len)
						{
							fw.inFs.Seek(-(read_len - write_len), SeekOrigin.Current);
							break;
						}
					}

					/* copy remaining data in inFs to outFs if rewrite mode */
					if (rewrite)
						fw.inFs.CopyTo(fw.outFs);
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			return 0;
		}
	}
}
