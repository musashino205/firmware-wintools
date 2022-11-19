using System;
using System.Collections.Generic;
using System.IO;

namespace firmware_wintools.Tools
{
	internal partial class Nec_BsdFFS : Tool
	{
		/* ツール情報　*/
		public override string name { get => "nec-bsdffs"; }
		public override string desc { get => "find and list/extract directories/files from NEC NetBSD FFS"; }
		public override string descFmt { get => "    {0}		: {1}"; }
		public override bool skipOFChk => true;


		internal bool isListMode = false;
		internal bool listToText = false;
		internal bool skipHardLink = true;
		internal string outTxt = null;
		internal string outDir = "necbsd-rootfs";

		private long supBlkOffset = 0;
		private long inoBlkOffset = 0;
		private long datBlkOffset = 0;
		private bool isBE = true;

		private const long defSupBlkOffset = 0x2000;

		/// <summary>
		/// rtkwebの機能ヘルプを表示します
		/// </summary>
		public void PrintHelp(int arg_idx)
		{
			Console.WriteLine("Usage: {0}nec-bsdffs [options...]\n" +
				desc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");   // 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
															// 共通オプション表示
			Program.PrintCommonOption(true);
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				"  -l\t\t\tshow list of directories/files instead of extracting\n" +
				"  -L <output>\t\toutput directory/file list to <output>\n" +
				"  -d [<directory>]\textract directories/files into <directory> (default: \"necbsd-rootfs\")\n" +
				"  -H\t\t\tdon't skip listing/extracting hard-link files\n");
		}

		internal long GetBlkOffset(long ExpectOffs)
		{
			return ExpectOffs - defSupBlkOffset + supBlkOffset;
		}

		/// <summary>
		/// nec-bsdffsメイン関数
		/// <para>NetBSDベースのNEC Aterm機のFFSを検出してリストを表示
		/// またはファイルを展開</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">Program内のメインプロパティ</param>
		/// <returns></returns>
		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			Firmware fw = new Firmware();
			DiskSuperBlk sBlk;
			List<Inode> inodes = new List<Inode>();
			/*
			 * 実際に存在していたinodeのリスト
			 * inodeブロックをなめていく際、inode番号がブロック内に格納されておらず
			 * 0始まりで振っていく為、ディレクトリのinodeエントリグループ内でファイルに
			 * 対し指定されている実際のinode番号と異なる
			 * 後でinodeがhardlinkされているか否かのチェック用
			 */
			List<uint> actInoList = new List<uint>();

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			Init_args(args, arg_idx);

			fw.inFInfo = new FileInfo(props.inFile);

			if (listToText && outTxt == null)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"no output file of list specifed");
				return 1;
			}

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					int read_len, ret = 0;
					uint i;
					long cur_off = 0;
					sBlk = new DiskSuperBlk();
					Inode ino = null;

					/* super block検索 */
					while (fw.inFs.Position < fw.inFs.Length) {
						cur_off = fw.inFs.Position;
						read_len = fw.inFs.Read(sBlk.buf, 0, DiskSuperBlk.SUPERBLK_LEN);
						if (read_len < DiskSuperBlk.SUPERBLK_LEN)
							break;

						ret = sBlk.CheckSuperBlk();
						if (ret == 0)
							break;
						else if (ret != -1)
							return ret;

						fw.inFs.Seek(-(DiskSuperBlk.SUPERBLK_LEN - sizeof(uint) * 2),
								SeekOrigin.Current);
						continue;
					}

					isBE = sBlk.isBE;
					supBlkOffset = cur_off;
					sBlk.PrintSuperBlk(cur_off);

					/* 以下2つはsuper block前に0x2000あることを期待する */
					inoBlkOffset = sBlk.inodeBlkCnt * 0x200;
					datBlkOffset = sBlk.dataBlkCnt * 0x200;
					fw.inFs.Seek(GetBlkOffset(inoBlkOffset), SeekOrigin.Begin);

					/* inodeパース */
					for (i = 0; i < (sBlk.dataBlkCnt - sBlk.inodeBlkCnt) * 4; i++)
					{
						ino = new Inode(this);

						read_len = fw.inFs.Read(ino.buf, 0, Inode.INODE_LEN);
						if (read_len != Inode.INODE_LEN)
							return 1;

						ino.inum = i;
						if (ino.ParseInode(isBE) != 0)
							continue;

						inodes.Add(ino);
					}

					foreach (Inode _ino in inodes)
					{
						Inode tmp = null;
						//_ino.PrintInode();

						if (_ino.ino_type == FFSFileInfo.INOFT_DIR &&
						    _ino.dirSetupDone == false)
						{
							//_ino.PrintInode();
							_ino.SetupDirInode(fw.inFs, isBE, in inodes, ref actInoList, out tmp);

							/*
							 * /dev に存在するはずがinodeエントリグループ内に該当inodeが
							 * 無いものが出る為、名前が未設定なら飛ばす
							 */
							if (tmp.ino_name != null)
								if (isListMode)
								{
									Stream listStream = listToText ?
											new FileStream(outTxt, FileMode.Create,
													FileAccess.Write, FileShare.None) :
											Console.OpenStandardOutput();
									using (listStream)
									using (StreamWriter sw = new StreamWriter(listStream))
										tmp.PrintDirFiles(in sw, in actInoList, null, isBE,
												skipHardLink);
								}
								else
								{
									if (!Directory.Exists(outDir))
										Directory.CreateDirectory(outDir);
									tmp.ExtractDirFiles(in fw.inFs, in actInoList, null, outDir,
											isBE, skipHardLink);
								}
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
