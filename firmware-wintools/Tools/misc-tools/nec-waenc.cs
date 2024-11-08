using System;
using System.Collections.Generic;
using System.IO;
using firmware_wintools.Lang;

namespace firmware_wintools.Tools
{
	internal class Nec_WAEnc : Tool
	{
		/* ツール情報　*/
		public override string name { get => "nec-waenc"; }
		public override string desc { get => "encode/decode NEC UNIVERGE WA series firmware"; }
		public override string descFmt { get => "    {0}\t\t: {1}"; }

		private const int PTN_LEN = 0x100;
		private const int PTN_BLK_LEN = 0x10;
		private const byte PTN_SEED = 0x2f;

		private byte Seed = PTN_SEED; /* default seed (WA1020/WA202x) */

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 's', PType = Param.PTYPE.BYTE, SetField = "Seed" },
		};

		private new void PrintHelp(int arg_idx)
		{
			Console.WriteLine("Usage: {0}nec-waenc [options...]\n" +
				desc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools "); // 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption(false);
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				"  -s <seed>\t\tspecify pattern seed at each 0x100\n" +
				"\t\t\tknown seeds:\n" +
				"\t\t\t- 0x2f: WA1020/WA202x (Default)\n" +
				"\t\t\t- 0xfa: WA151x\n" +
				"\t\t\t- 0xc6: WA2511E/WA261x\n");
		}

		private int
		GeneratePattern(out byte[] pattern)
		{
			byte[] __pattern = new byte[PTN_LEN * 2];
			int tmp;

			pattern = null;
			__pattern[0] = PTN_SEED;
			for (int i = 1; i < PTN_LEN; i++)
			{
				if (i % PTN_BLK_LEN == 0)
				{
					__pattern[i]
						= Convert.ToByte((__pattern[i - 1] - 1) & 0xff);
					continue;
				}
				tmp = 0xf - i % PTN_BLK_LEN;
				tmp |= ((0 - i % PTN_BLK_LEN) & 0xf) << 4;
				tmp += __pattern[i - 1] & 0xf0;
				__pattern[i] = Convert.ToByte(tmp & 0xff);
			}
			Buffer.BlockCopy(__pattern, 0, __pattern, PTN_LEN, PTN_LEN);

			/* 機種パターン0x100での最初の値をデフォルトパターンから探す */
			tmp = Array.FindIndex(__pattern, 0, PTN_LEN, x=>x == Seed);
			if (tmp < 0)
				return -1;

			pattern = new byte[PTN_LEN];
			/* デフォルトパターン x2 から機種パターン最初の値以降0x100をコピー */
			Buffer.BlockCopy(__pattern, tmp, pattern, 0, PTN_LEN);

			return 0;
		}

		/// <summary>
		/// nec-waencメイン関数
		/// <para>コマンドライン引数とメインプロパティから、xorを用いた
		/// NEC UNIVERGE WA シリーズ用ファームウェアのエンコード/デコードを行います
		/// </para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">Program内メインプロパティ</param>
		/// <returns>実行結果</returns>
		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			Firmware fw = new Firmware();
			int ret, read_len;

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return ret;

			if (props.Help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ret = GeneratePattern(out byte[] pattern);
			if (ret < 0)
			{
				Console.Error.WriteLine(
					Resource.Main_Error_Prefix +
					"failed to generate XOR pattern");
				return ret;
			}

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				using (fw.outFs = new FileStream(props.OutFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
				{
					int p_off = 0;
					fw.data = new byte[0x10000];

					while ((read_len = fw.inFs.Read(fw.data, 0, fw.data.Length)) > 0)
					{
						p_off = Utils.XorData(ref fw.data, read_len, pattern, p_off);

						fw.outFs.Write(fw.data, 0, read_len);
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
