using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace firmware_wintools.Tools
{
	internal class XorImage : Tool
	{
		/* ツール情報　*/
		public override string name { get => "xorimage"; }
		public override string desc { get => Lang.Tools.XorImageRes.FuncDesc; }
		public override string descFmt { get => Lang.Tools.XorImageRes.Main_FuncDesc_Fmt; }
		public override string resName => "XorImageRes";


		private long Length = long.MinValue;
		private long Offset = 0;
		private string Pattern = "12345678";
		private bool IsHex = false;
		private bool Rewrite = false;
		private string BinPattern = null;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 'p', PType = Param.PTYPE.STR, SetField = "Pattern", HelpKey = "Help_Options_Patern" },
			new Param() { PChar = 'P', PType = Param.PTYPE.STR, SetField = "BinPattern", HelpKey = "Help_Options_PatternBin" },
			new Param() { PChar = 'x', PType = Param.PTYPE.BOOL, SetField = "IsHex", HelpKey = "Help_Options_Hex" },
			new Param() { PChar = 'l', PType = Param.PTYPE.LONG, SetField = "Length", HelpKey = "Help_Options_Length" },
			new Param() { PChar = 'O', PType = Param.PTYPE.LONG, SetField = "Offset", HelpKey = "Help_Options_Offset" },
			new Param() { PChar = 'r', PType = Param.PTYPE.BOOL, SetField = "Rewrite", HelpKey = "Help_Options_Rewrite" }
		};

		/// <summary>
		/// xorimageの実行情報を表示します
		/// </summary>
		/// <param name="props"></param>
		private void PrintInfo(long datalen, in byte[] ptnAry)
		{
			Console.WriteLine(Lang.Tools.XorImageRes.Info);
			Console.WriteLine(Lang.Tools.XorImageRes.Info_Pattern,
						BinPattern != null ?
							BitConverter.ToString(ptnAry).Replace("-", "") :
							Pattern);
			Console.WriteLine(Lang.Tools.XorImageRes.Info_Hex, IsHex.ToString());
			Console.WriteLine(Lang.Tools.XorImageRes.Info_len, datalen);
			Console.WriteLine(Lang.Tools.XorImageRes.Info_offset, Offset);
			Console.WriteLine(Lang.Tools.XorImageRes.Info_Rewrite, Rewrite);
		}

		private int
		SetupPattern(out byte[] ptnAry, out int p_len)
		{
			FileInfo ptnBinInfo = null;
			int hexPtn_maxLen = 0x100;

			ptnAry = null;
			p_len = 0;

			/* パターンバイナリ */
			if (BinPattern != null)
			{
				/*
				 * - BinPatternが空文字
				 * - ファイルが存在しない
				 * - ファイルサイズが 0x100 (256 bytes) 超
				 */
				if (BinPattern.Length == 0 ||
				    !File.Exists(BinPattern) ||
				    (ptnBinInfo = new FileInfo(BinPattern)).Length > hexPtn_maxLen)
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
					using (FileStream ptnFs = new FileStream(BinPattern, FileMode.Open,
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

				IsHex = true;
				p_len = ptnAry.Length;
				return 0;
			}

			/* テキストパターン */
			p_len = Pattern.Length;
			if (p_len == 0)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Error_Prefix + Lang.Tools.XorImageRes.Error_InvalidPatternLen);
				return 1;
			}

			if (IsHex)
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
					ptnAry[i] = Convert.ToByte(Pattern.Substring(i * 2, 2), 16);
			}
			else
				ptnAry = Encoding.ASCII.GetBytes(Pattern);

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
			byte[] ptnAry;
			Firmware fw = new Firmware()
			{
				data = new byte[4096]
			};
			int ret;

			if (props.Help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return ret;

			if (SetupPattern(out ptnAry, out p_len) != 0)
				return 1;

			fw.inFInfo = new FileInfo(props.InFile);

			/* check offset/length */
			if (Offset > fw.inFInfo.Length)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.XorImageRes.Warning_LargeOffset);
				Offset = 0;
			}

			if (Length == long.MinValue)
				Length = fw.inFInfo.Length - Offset;
			/*
			 * - lenが0以下であるか
			 * - lenが入力ファイルの長さからオフセットを引いたものより大きいか
			 */
			if (Length <= 0 ||
			    Length > fw.inFInfo.Length - Offset)
			{
				Console.Error.WriteLine(
					Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.XorImageRes.Warning_InvalidLength);
				Length = fw.inFInfo.Length - Offset;
			}
			/* check offset/length end */

			if (!props.Quiet)
				PrintInfo(Length, in ptnAry);

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				using (fw.outFs = new FileStream(props.OutFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
				{
					if (Rewrite && Offset > 0)
					{
						fw.inFs.CopyTo(fw.outFs);
						/* 書き換え開始位置までseek */
						fw.outFs.Seek(Offset, SeekOrigin.Begin);
					}

					/* inFs.Positionをoffsetに移動 */
					fw.inFs.Seek(Offset, SeekOrigin.Begin);

					while ((read_len = fw.inFs.Read(fw.data, 0, fw.data.Length)) > 0)
					{
						write_len = (Length < read_len) ? (int)Length : read_len;

						p_off = Utils.XorData(ref fw.data, write_len, ptnAry, p_off);
						fw.outFs.Write(fw.data, 0, write_len);

						Length -= write_len;
						if (Length <= 0)
						{
							fw.inFs.Seek(-(read_len - write_len), SeekOrigin.Current);
							break;
						}
					}
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
