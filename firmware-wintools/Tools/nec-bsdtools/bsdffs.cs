using System;
using System.Collections.Generic;
using System.IO;

namespace firmware_wintools.Tools
{
	internal partial class BsdFFS : Tool
	{
		/* ツール情報　*/
		public override string name { get => "bsdffs"; }
		public override string desc { get => "find and list/extract directories/files from BSD FFS"; }
		public override string descFmt { get => "    {0}		: {1}"; }
		public override bool skipOFChk => true;


		private long supBlkOffset = 0;
		private long inoBlkOffset = 0;
		private long datBlkOffset = 0;
		private bool isBE = true;

		private const long defSupBlkOffset = 0x2000;

		private bool IsList = false;
		private bool IsListToText = false;
		private bool SkipHardLink = true;
		private bool Search4B = false;
		private string OutText = null;
		private string OutDir = "bsdffs-root";
		private string OutFsBin = null;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param()	{ PChar = 'd', PType = Param.PTYPE.STR, SetField = "OutDir" },
			new Param() { PChar = 'f', PType = Param.PTYPE.STR, SetField = "OutFsBin" },
			new Param() { PChar = 'H', PType = Param.PTYPE.BOOL, SetField = "!SkipHardLink" },
			new Param() { PChar = 'L', PType = Param.PTYPE.STR, SetField = "OutText", SetBool = "IsListToText" },
			new Param() { PChar = 'l', PType = Param.PTYPE.BOOL, SetField = "IsList" },
			new Param() { PChar = '4', PType = Param.PTYPE.BOOL, SetField = "Search4B" }
		};

		/// <summary>
		/// bsdffsの機能ヘルプを表示します
		/// </summary>
		public new void PrintHelp(int arg_idx)
		{
			Console.WriteLine("Usage: {0}bsdffs [options...]\n" +
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
				"  -f <output>[:<size>]\tcut out filesystem binary to <output> with <size> (default: 32MB)\n" +
				"  -H\t\t\tdon't skip listing/extracting hard-link files\n" +
				"  -4\t\t\tsearch superblock on each 4 bytes instead of 8 bytes\n");
		}

		internal long GetBlkOffset(long ExpectOffs)
		{
			return ExpectOffs - defSupBlkOffset + supBlkOffset;
		}

		/// <summary>
		/// bsdffsメイン関数
		/// <para>NetBSDベースのNEC Aterm機のFFSを検出してリストを表示
		/// またはファイルを展開</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">Program内のメインプロパティ</param>
		/// <returns></returns>
		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			Firmware fw = new Firmware();
			SuperBlock sblk;
			List<Inode> inodes = new List<Inode>();
			/*
			 * 実際に存在していたinodeのリスト
			 * inodeブロックをなめていく際、inode番号がブロック内に格納されておらず
			 * 0始まりで振っていく為、ディレクトリのinodeエントリグループ内でファイルに
			 * 対し指定されている実際のinode番号と異なる
			 * 後でinodeがhardlinkされているか否かのチェック用
			 */
			List<uint> actInoList = new List<uint>();
			int ret;

			if (props.Help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return ret;

			fw.inFInfo = new FileInfo(props.InFile);

			if (IsListToText && OutText == null)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"no output file of list specifed");
				return 1;
			}

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					int read_len;
					uint i;
					long cur_off = 0;
					sblk = new SuperBlock();
					Inode ino = null;

					/* super block検索 */
					while (fw.inFs.Position < fw.inFs.Length) {
						cur_off = fw.inFs.Position;
						read_len = fw.inFs.Read(sblk.Data, 0, SuperBlock.LENGTH);
						if (read_len < SuperBlock.LENGTH)
							break;

						if (sblk.DeserializeProps(0, sblk.GetEndian()) > 0 &&
						    sblk.Evaluate())
							break;

						fw.inFs.Seek(-(SuperBlock.LENGTH
								- sizeof(uint) * (Search4B ? 1 : 2)),
								SeekOrigin.Current);
					}

					/*
					 * if not found
					 * (current offset + superblock length is
					 *  larger than input file)
					 */
					if (cur_off + SuperBlock.LENGTH > fw.inFs.Length)
					{
						Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
								"no super block found");
						return ret;
					}

					isBE = sblk.endian == HeaderFooter.Endian.BE ? true : false;
					supBlkOffset = cur_off;
					sblk.PrintSuperBlock(cur_off);

					/* ファイルシステム切り出し */
					if (OutFsBin != null)
					{
						string[] finfo = OutFsBin.Split(':');
						string dir = Path.GetDirectoryName(finfo[0]);
						long len = sblk.size * SuperBlock.BLKSZ;

						if (dir.Length > 0 && !Directory.Exists(dir))
						{
							Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
									"target directory for cut out fs binary doesn't exit");
							return -1;
						}

						/* サイズ指定時 */
						if (finfo.Length >= 2 &&
							(finfo[1] != null && finfo[1].Length > 0))
						{
							if (!Utils.StrToLong(finfo[1], out len,
												 System.Globalization.NumberStyles.None))
							{
								Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
										"failed to convert length");
								return -1;
							}

							if (len > fw.inFs.Length - supBlkOffset)
							{
								Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
										"specifed length is longer than available");
								return -1;
							}
						}
						/* サイズ未指定時 */
						else
						{
							if (fw.inFs.Length < len - defSupBlkOffset)
							{
								Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
										"input file is smaller than FS size");
								return -1;
							}

							if (supBlkOffset < defSupBlkOffset)
								len -= defSupBlkOffset;
						}

						fw.inFs.Seek(
							(supBlkOffset < defSupBlkOffset) ?
								supBlkOffset : supBlkOffset - defSupBlkOffset,
							SeekOrigin.Begin);
						using (FileStream fs = new FileStream(finfo[0], FileMode.Create,
								FileAccess.Write, FileShare.None))
						{
							byte[] buf = new byte[0x10000];
							int readlen;

							readlen = len < buf.Length ? Convert.ToInt32(len) : buf.Length;
							while ((readlen = fw.inFs.Read(buf, 0, readlen)) > 0)
							{
								fs.Write(buf, 0, readlen);
								len -= readlen;
								readlen = len < buf.Length ? Convert.ToInt32(len) : buf.Length;
							}
						}

						return 0;
					}

					/* 以下2つはsuper block前に0x2000あることを期待する */
					inoBlkOffset = sblk.iblkno * SuperBlock.BLKSZ;
					datBlkOffset = sblk.dblkno * SuperBlock.BLKSZ;
					fw.inFs.Seek(GetBlkOffset(inoBlkOffset), SeekOrigin.Begin);

					/* inodeパース */
					for (i = 0; i < (sblk.dblkno - sblk.iblkno) * 4; i++)
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
							_ino.SetupDirInode(fw.inFs, isBE, in inodes, ref actInoList, out tmp, sblk.nindir);

							/*
							 * /dev に存在するはずがinodeエントリグループ内に該当inodeが
							 * 無いものが出る為、名前が未設定なら飛ばす
							 */
							if (tmp.ino_name != null)
								if (IsList || IsListToText)
								{
									Stream listStream = IsListToText ?
											new FileStream(OutText, FileMode.Create,
													FileAccess.Write, FileShare.None) :
											Console.OpenStandardOutput();
									using (listStream)
									using (StreamWriter sw = new StreamWriter(listStream))
										tmp.PrintDirFiles(in sw, in actInoList, null, isBE,
												SkipHardLink);
								}
								else
								{
									if (!Directory.Exists(OutDir))
										Directory.CreateDirectory(OutDir);
									tmp.ExtractDirFiles(in fw.inFs, in actInoList, sblk.nindir, null, OutDir,
											isBE, SkipHardLink);
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
