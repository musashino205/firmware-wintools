﻿using System;
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
			public bool half;
		}

		/// <summary>
		/// nec-encの機能ヘルプを表示します
		/// </summary>
		private void PrintHelp(int arg_idx)
		{
			Console.WriteLine(Lang.Tools.NecEncRes.Help_Usage +
				Lang.Tools.NecEncRes.FuncDesc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");	// 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption();
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				Lang.Tools.NecEncRes.Help_Options_k);
		}

		/// <summary>
		/// nec-encの実行情報を表示します
		/// </summary>
		/// <param name="props">nec-encの機能プロパティ</param>
		private void PrintInfo(Properties subprops)
		{
			Console.WriteLine(Lang.Tools.NecEncRes.Info);
			Console.WriteLine(Lang.Tools.NecEncRes.Info_key,
				!subprops.half ? subprops.key : "(none)");
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
		private int XorPattern(ref byte[] data, int len, byte[] key, int k_len, int k_off)
		{
			int data_pos = 0;

			while (len-- > 0)
			{
				data[data_pos] ^= key[k_off];
				data_pos++;
				k_off = (k_off + 1) % k_len;
			}

			return k_off;
		}

		/// <summary>
		/// patternを用いて対象データのxorを行います
		/// <para>対象データとpatternは長さが同一である必要があります</para>
		/// </summary>
		/// <param name="data">xor対象データ</param>
		/// <param name="len">xor対象データの長さ</param>
		/// <param name="pattern">xorに用いるpattern</param>
		private void XorData(ref byte[] data, int len, in byte[] pattern)
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
		public int Do_NecEnc(string[] args, int arg_idx, Program.Properties props)
		{
			int max_key_len = 16;
			int pattern_len = 251;
			int read_len;
			int ptn = 1;
			int k_off = 0;
			int k_len = 0;
			byte[] key = new byte[] {};
			byte[] buf_pattern = new byte[4096];
			byte[] buf = new byte[4096];
			Properties subprops = new Properties();

			ToolsArgMap argMap = new ToolsArgMap();
			argMap.Init_args_NecEnc(args, ref subprops);

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			if (!subprops.half)
			{
				if (subprops.key == null)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.NecEncRes.Error_NoKey);
					return 1;
				}

				k_len = subprops.key.Length;
				if (k_len == 0 || k_len > max_key_len)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.NecEncRes.Error_InvalidKeyLen,
						max_key_len);
					return 1;
				}

				key = Encoding.ASCII.GetBytes(subprops.key);
			}

			if (!props.quiet)
				PrintInfo(subprops);

			FileStream inFs;
			FileStream outFs;
			FileStream patFs = null;
			FileStream xpatFs = null;
			FileMode outFMode =
				File.Exists(props.outFile) ? FileMode.Truncate : FileMode.Create;
			try
			{
				inFs = new FileStream(props.inFile, FileMode.Open, FileAccess.Read, FileShare.Read);
				outFs = new FileStream(props.outFile, outFMode, FileAccess.Write, FileShare.None);

				if (props.debug)
				{
					patFs = new FileStream(@"pattern.bin", FileMode.OpenOrCreate, FileAccess.Write);
					if (!subprops.half)
						xpatFs = new FileStream(@"pattern.xor", FileMode.OpenOrCreate, FileAccess.Write);
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
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

				if (!subprops.half)
				{
					k_off = XorPattern(ref buf_pattern, read_len, key, k_len, k_off);
					if (props.debug)
						xpatFs.Write(buf_pattern, 0, read_len);
				}

				XorData(ref buf, read_len, in buf_pattern);

				outFs.Write(buf, 0, read_len);
			}

			inFs.Close();
			outFs.Close();
			if (props.debug)
			{
				patFs.Close();
				if (!subprops.half)
					xpatFs.Close();
			}

			return 0;
		}
	}
}
