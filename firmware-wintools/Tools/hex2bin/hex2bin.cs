using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace firmware_wintools.Tools
{
	internal class Hex2Bin : Tool
	{
		/* ツール情報　*/
		public override string name { get => "hex2bin"; }
		public override string desc { get => "convert text-based hex string to binary"; }
		public override string descFmt { get => "    {0}		: {1}"; }


		private bool IsTable = false;
		private int Offset = 0;
		private int Width = 16;
		private bool SkipFirstBlk = false;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 'w', PType = Param.PTYPE.INT, SetField = "Width" },
			new Param() { PChar = 't', PType = Param.PTYPE.BOOL, SetField = "IsTable" },
			new Param() { PChar = 'H', PType = Param.PTYPE.BOOL, SetField = "SkipFirstBlk" },
			new Param() { PChar = 'O', PType = Param.PTYPE.INT, SetField = "Offset" }
		};

		/// <summary>
		/// rtkwebの機能ヘルプを表示します
		/// </summary>
		public new void PrintHelp(int arg_idx)
		{
			Console.WriteLine("Usage: {0}hex2bin [options...]\n" +
				desc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");	// 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption(true);
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				"  -w			specify column width of table\n" +
				"  -t			convert table format (ex.: hexdump, od)\n" +
				"  -H			skip first block of line on table mode\n" +
				"  -O <offset>		start conversion from <offset> on table mode\n");
		}

		/// <summary>
		/// hex2binメイン関数
		/// <para>コマンドライン引数とメインプロパティから、16新数値のテキストデータを
		/// バイナリデータに変換します</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">Program内のメインプロパティ</param>
		/// <returns></returns>
		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			Firmware fw = new Firmware();
			int ret;

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return ret;

			try
			{
				using (StreamReader sr = new StreamReader(props.inFile, Encoding.ASCII))
				using (fw.outFs = new FileStream(props.outFile, FileMode.Create,
							FileAccess.Write, FileShare.Read))
				{
					char[] charBuf = new char[0x10000];
					string line;
					int i = 0;

					while (sr.Peek() > -1)
					{
						if (IsTable)
						{
							i++;
							if (i <= Offset)
							{
								sr.ReadLine();
								continue;
							}

							line = sr.ReadLine();
							if (SkipFirstBlk)
							{
								int firstSpace = line.IndexOf(' ');
								if (firstSpace == -1)
									break;

								line = line.Remove(0, firstSpace);
							}
						}
						else
						{
							int readLen;

							readLen = sr.ReadBlock(charBuf, 0, charBuf.Length);
							line = new string(charBuf, 0, readLen);
						}

						line = line.Replace(" ", "");
						if (!IsTable)
							Width = line.Length / 2;

						if (!Utils.StrToByteArray(ref line, out byte[] _buf, 0, Width))
						{
							Console.Error.Write(Lang.Resource.Main_Error_Prefix +
									"invalid character detected \"{0}\"", line);
							if (IsTable)
								Console.Error.WriteLine(" at line {0}", i);
							return 1;
						}

						fw.outFs.Write(_buf, 0, Width);
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
