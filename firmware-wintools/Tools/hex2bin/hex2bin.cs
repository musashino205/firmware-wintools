using System;
using System.IO;
using System.Text;

namespace firmware_wintools.Tools
{
	internal partial class Hex2Bin : Tool
	{
		/* ツール情報　*/
		public override string name { get => "hex2bin"; }
		public override string desc { get => "convert text-based hex string to binary"; }
		public override string descFmt { get => "    {0}		: {1}"; }


		bool isTable = false;
		int offset = 0;
		int width = 16;
		bool skipFirstBlock = false;

		/// <summary>
		/// rtkwebの機能ヘルプを表示します
		/// </summary>
		public void PrintHelp(int arg_idx)
		{
			Console.WriteLine("Usage: {0}hex2bin [options...]\n" +
				desc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");	// 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption(true);
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				"  -s			specify column width of table\n" +
				"  -t			convert table format (ex.: hexdump, od)\n" +
				"  -H			skip first block of line on table mode\n" +
				"  -O <offset>		start conversion from <offset> on table mode\n");
		}

		private int LineToFile(ref string line, ref FileStream fs, int len)
		{
			if (!Utils.StrToByteArray(ref line, out byte[] buf, 0, len))
				return 1;

			fs.Write(buf, 0, len);
			return 0;
		}

		/// <summary>
		/// rtkwebメイン関数
		/// <para>コマンドライン引数とメインプロパティから、xorによりファームウェアの
		/// エンコード/デコード を行います</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">Program内のメインプロパティ</param>
		/// <returns></returns>
		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			Firmware fw = new Firmware();

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			Init_args(args, arg_idx);

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
						if (isTable)
						{
							i++;
							if (i <= offset)
							{
								sr.ReadLine();
								continue;
							}

							line = sr.ReadLine();
							if (skipFirstBlock)
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
						if (!isTable)
							width = line.Length / 2;

						if (LineToFile(ref line, ref fw.outFs, width) != 0)
						{
							Console.Error.Write(Lang.Resource.Main_Error_Prefix +
									"invalid character detected \"{0}\"", line);
							if (isTable)
								Console.Error.WriteLine(" at line {0}", i);
							return 1;
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
