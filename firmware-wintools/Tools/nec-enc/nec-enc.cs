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

		private const int KEY_MAX_LEN = 32;
		private const byte PTN_MAX = 251;

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

		private void
		XorData(ref byte[] data, int len, byte[] key, ref int k_off, ref byte ptn)
		{
			for (int i = 0; len-- > 0;
			     i++, ptn = (byte)(ptn % PTN_MAX + 1), k_off %= key.Length)
			{
				data[i] ^= ptn;
				if (!Half)
					data[i] ^= key[k_off++];
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
			int read_len;
			int k_off = 0;
			Firmware fw = new Firmware();
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

				if (Key.Length == 0 || Key.Length > KEY_MAX_LEN)
				{
					Console.Error.WriteLine(
						Lang.Resource.Main_Error_Prefix + Lang.Tools.NecEncRes.Error_InvalidKeyLen,
						KEY_MAX_LEN);
					return 1;
				}
			}

			if (!props.quiet)
				PrintInfo();

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				using (fw.outFs = new FileStream(props.outFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
				{
					fw.data = new byte[0x1000];
					byte ptn = 1;

					while ((read_len = fw.inFs.Read(fw.data, 0, fw.data.Length)) > 0)
					{
						XorData(ref fw.data, read_len, Key, ref k_off, ref ptn);

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
