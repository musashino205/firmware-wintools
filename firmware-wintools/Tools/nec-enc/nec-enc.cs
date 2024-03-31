using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace firmware_wintools.Tools
{
	internal class Nec_Enc : Tool
	{
		/* ツール情報　*/
		public override string name { get => "nec-enc"; }
		public override string desc { get => Lang.Tools.NecEncRes.FuncDesc; }
		public override string descFmt { get => Lang.Tools.NecEncRes.Main_FuncDesc_Fmt; }
		public override string resName => "NecEncRes";

		private byte[] Key = null;
		private bool Half = false;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 'k', PType = Param.PTYPE.BARY, SetField = "Key", HelpKey = "Help_Options_k" },
			new Param() { PChar = 'H', PType = Param.PTYPE.BOOL, SetField = "Half" }
		};

		/// <summary>
		/// nec-encの実行情報を表示します
		/// </summary>
		/// <param name="props">nec-encの機能プロパティ</param>
		private void PrintInfo()
		{
			Console.WriteLine(Lang.Tools.NecEncRes.Info);
			Console.WriteLine(Lang.Tools.NecEncRes.Info_key,
				!Half ? Encoding.ASCII.GetString(Key) : "(none)");
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
		private static int XorPattern(ref byte[] data, int len, byte[] key, int k_len, int k_off)
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
		private static void XorData(ref byte[] data, int len, in byte[] pattern)
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
		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			const int MAX_KEY_LEN = 32;
			int read_len;
			int k_off = 0;
			NecEncFirmware fw = new NecEncFirmware() {
				data = new byte[4096],
				buf_ptn = new byte[4096]
			};
			int ret;

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return ret;

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			if (!Half)
			{
				if (Key == null)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.NecEncRes.Error_NoKey);
					return 1;
				}

				if (Key.Length == 0 || Key.Length > MAX_KEY_LEN)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.NecEncRes.Error_InvalidKeyLen,
						MAX_KEY_LEN);
					return 1;
				}
			}

			if (!props.quiet)
				PrintInfo();

			try
			{
				FileStream patFs = null;
				FileStream xpatFs = null;

				if (props.debug)
				{
					patFs = new FileStream(@"pattern.bin",
							FileMode.Create, FileAccess.Write);
					if (!Half)
						xpatFs = new FileStream(@"pattern.xor",
							FileMode.Create, FileAccess.Write);
				}

				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				using (fw.outFs = new FileStream(props.outFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
				{
					while ((read_len = fw.inFs.Read(fw.data, 0, fw.data.Length)) > 0)
					{
						fw.GenerateBasePattern(read_len);

						if (props.debug)
							patFs.Write(fw.buf_ptn, 0, read_len);

						if (!Half)
						{
							k_off = XorPattern(ref fw.buf_ptn, read_len, Key, Key.Length, k_off);
							if (props.debug)
								xpatFs.Write(fw.buf_ptn, 0, read_len);
						}

						XorData(ref fw.data, read_len, in fw.buf_ptn);

						fw.outFs.Write(fw.data, 0, read_len);
					}
				}

				if (props.debug)
				{
					patFs.Close();
					if (!Half)
						xpatFs.Close();
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
