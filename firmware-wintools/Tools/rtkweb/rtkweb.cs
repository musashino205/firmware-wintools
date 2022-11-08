using System;
using System.IO;
using System.Net;
using System.Text;

namespace firmware_wintools.Tools
{
	internal partial class RtkWeb : Tool
	{
		/* ツール情報　*/
		public override string name { get => "rtkweb"; }
		public override string desc { get => "decode Realtek Web Data binary"; }
		public override string descFmt { get => "    {0}		: {1}"; }
		public override bool skipOFChk => true;


		private readonly int FINFO_LEN = 0x40;
		private readonly int FINFO_FNAME_LEN = 0x3c;

		string dir = "webdata";

		/// <summary>
		/// rtkwebの機能ヘルプを表示します
		/// </summary>
		public void PrintHelp(int arg_idx)
		{
			Console.WriteLine("Usage: {0}rtkweb [options...]\n" +
				desc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");	// 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption(true);
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				"  -d <directory>	deploy web files into <directory> (default: \"webdata\")\n");
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
			int read_len, fcnt = 0;
			Firmware fw = new Firmware();

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			Init_args(args, arg_idx);

			fw.inFInfo = new FileInfo(props.inFile);

			if (dir.Length == 0) {
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"invalid output directory specified");
				return 1;
			}
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					string fname, fdir, fbase;
					uint flen = 0;
					int nameEnd = 0;

					while (fw.inFs.Position < fw.inFs.Length) {
						fw.data = new byte[FINFO_LEN];
						read_len = fw.inFs.Read(fw.data, 0, FINFO_LEN);

						if (read_len < FINFO_LEN) {
							Console.Error.WriteLine(Lang.Resource.Main_Warning_Prefix +
										"fileinfo field is too short! {0} bytes (ofs: 0x{1:x08})",
										read_len, fw.inFs.Position);
							break;
						}

						nameEnd = Array.IndexOf<byte>(fw.data, 0, 0, FINFO_FNAME_LEN);

						fname = Encoding.ASCII.GetString(fw.data, 0, nameEnd);
						flen = (uint)IPAddress.NetworkToHostOrder(
										BitConverter.ToInt32(fw.data, FINFO_FNAME_LEN));

						fdir = Path.GetDirectoryName(fname);
						fbase = Path.GetFileName(fname);

						if (fdir.Length != 0 && !Directory.Exists(dir + "/" + fdir))
							Directory.CreateDirectory(dir + "/" + fdir);

						fw.data = new byte[(int)flen];
						read_len = fw.inFs.Read(fw.data, 0, (int)flen);
						if (read_len < flen) {
							Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
										"failed to read data with length {0} bytes",
										flen);
						}

						using (FileStream outfile = new FileStream(dir + "/" + fname,
											FileMode.Create, FileAccess.Write, FileShare.Read))
						{
							outfile.Write(fw.data, 0, (int)flen);
						}

						if (props.debug)
							Console.WriteLine("{0,45}: size-> 0x{1:X08} ({1,8} bytes), offset-> 0x{2:X08}",
									fname, flen, fw.inFs.Position);
//						else
//							Console.WriteLine("{0,45}: {1,8} bytes",
//								fname, flen);

						fcnt++;
					}
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			Console.WriteLine("{0} files", fcnt);

			return 0;
		}
	}
}
