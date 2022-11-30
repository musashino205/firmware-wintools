/*
 * BSD FFS (Fast File System)
 *
 * super block: struct fs (fs.h)
 * inode: struct ufs1_dinode (dinode.h)
 *
 * https://ja.osdn.net/projects/ffsdrv/releases/
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace firmware_wintools.Tools
{
	internal partial class Nec_BsdFFS
	{
		private class Inode
		{
			internal ushort mode = 0;
			internal short nlink = 0;
			/* 0x4-0x7: unused */
			internal ulong len = 0;
			internal int atime = 0;
			internal int atime_ns = 0;
			internal int mtime = 0;
			internal int mtime_ns = 0;
			internal int ctime = 0;
			internal int ctime_ns = 0;
			internal int[] directDiskBlks = new int[12];
			internal int[] indirectDiskBlks = new int[3];
			internal uint flags = 0;
			internal uint blocks = 0;
			/* 0x6c-0x6f: unused */
			internal uint uid = 0;
			internal uint gid = 0;


			[NonSerialized]
			internal static readonly int INODE_LEN = 0x80;
			[NonSerialized]
			internal byte[] buf = new byte[INODE_LEN];
			[NonSerialized]
			internal uint inum = 0;
			[NonSerialized]
			internal uint ino_type = 0;
			[NonSerialized]
			internal uint ino_perm = 0;
			[NonSerialized]
			internal string ino_name = null;
			[NonSerialized]
			internal string ino_lnktarget = null;
			[NonSerialized]
			internal List<Inode> dirFileEnt = null;
			[NonSerialized]
			internal bool dirSetupDone = false;
			[NonSerialized]
			private Nec_BsdFFS parent = null;

			public Inode(Nec_BsdFFS _parent)
			{
				parent = _parent;
			}

			/// <summary>
			/// inodeブロックからinode情報をパース
			/// </summary>
			/// <param name="isBE"></param>
			/// <returns></returns>
			internal int ParseInode(bool isBE)
			{
				FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance |
						BindingFlags.NonPublic);
				uint val32;
				int i = 0;
				long val64;

				foreach (FieldInfo field in fields)
				{
					if (field.IsNotSerialized)
						continue;
					if (field.Name == "len" ||
					    field.Name == "uid")
						i += sizeof(uint);
					/* (in)directDiskBlks */
					if (field.FieldType.IsArray)
					{
						int[] a = (int[])field.GetValue(this);

						for (int j = 0; j < a.Length; j++)
						{
							a[j] = isBE ?
									Utils.BE32toHost(BitConverter.ToInt32(buf, i)) :
									Utils.LE32toHost(BitConverter.ToInt32(buf, i));
							a[j] *= 0x200;
							i += sizeof(int);
						}
						continue;
					}
					/* len */
					else if (field.FieldType == typeof(ulong))
					{
						val64 = isBE ?
								Utils.BE64toHost(BitConverter.ToInt64(buf, i)) :
								Utils.LE64toHost(BitConverter.ToInt64(buf, i));
						field.SetValue(this, (ulong)val64);
						i += sizeof(ulong);
					}
					else
					{
						val32 = isBE ?
									(uint)Utils.BE32toHost(BitConverter.ToInt32(buf, i)) :
									(uint)Utils.LE32toHost(BitConverter.ToInt32(buf, i));
						/* mode, nlink */
						if (field.FieldType == typeof(short) ||
						    field.FieldType == typeof(ushort))
						{
							ushort tmp = isBE ?
									Convert.ToUInt16(val32 >> 16) :
									Convert.ToUInt16(val32 & ushort.MaxValue);
							if (field.FieldType == typeof(ushort))
								field.SetValue(this, tmp);
							else
								field.SetValue(this, Convert.ToInt16(tmp));
							i += sizeof(short);
							continue;
						}

						if (field.FieldType == typeof(uint))
							field.SetValue(this, val32);
						else
							field.SetValue(this, Convert.ToInt32(val32));
						i += sizeof(uint);
					}
				}

				if (mode == 0)
					return -1;
				ino_type = mode & FFSFileInfo.INOFTYPE_MASK;
				ino_perm = mode & FFSFileInfo.FPERM_MASK;
				/* symlinkの場合(in)direct blocksの代わりにリンク先ファイルのパスが存在 */
				if (ino_type == FFSFileInfo.INOFT_LNK)
				{
					int zeroIdx = Array.IndexOf<byte>(buf, 0, 0x28, 0x3c);

					if (zeroIdx > 0x29)
						ino_lnktarget = Encoding.ASCII.GetString(buf, 0x28, zeroIdx - 0x28);
				}

				return 0;
			}

			/// <summary>
			/// inodeの対象データを取得
			/// <para>データ長大きい場合時間かかる為できれば改善</para>
			/// </summary>
			/// <param name="inFs"></param>
			/// <param name="buf"></param>
			/// <param name="isBE"></param>
			/// <returns></returns>
			internal int GetInodeData(in FileStream inFs, out byte[] buf, bool isBE)
			{
				byte[] tmp = new byte[sizeof(uint)];
				long offset;
				uint val;
				int i, j;

				buf = null;

				/* no data */
				if (blocks <= 0)
					return -1;

				buf = new byte[blocks * 0x200];
				for (i = 0; i < blocks; i++)
				{
					if (i < 0x60)
					{
						offset = directDiskBlks[i >> 3] + 0x200 * (i & 0x7);
						inFs.Seek(parent.GetBlkOffset(offset), SeekOrigin.Begin);
						inFs.Read(buf, i * 0x200, 0x200);
					}
					else if (i < 0x1060)
					{
						j = i - 0x60;
						offset = indirectDiskBlks[0] + 4 * ((j >> 3) & 0x1ff);
						inFs.Seek(parent.GetBlkOffset(offset), SeekOrigin.Begin);
						inFs.Read(tmp, 0, sizeof(uint));
						val = isBE ?
							(uint)Utils.BE32toHost(BitConverter.ToInt32(tmp, 0)) :
							(uint)Utils.LE32toHost(BitConverter.ToInt32(tmp, 0));
						offset = val * 0x200 + 0x200 * (j & 0x7);
						inFs.Seek(parent.GetBlkOffset(offset), SeekOrigin.Begin);
						inFs.Read(buf, i * 0x200, 0x200);
					}
					else
					{
						j = i - 0x1060;
						offset = indirectDiskBlks[1] + 4 * ((j >> 12) & 0x1ff);
						inFs.Seek(parent.GetBlkOffset(offset), SeekOrigin.Begin);
						inFs.Read(tmp, 0, sizeof(uint));
						val = isBE ?
							(uint)Utils.BE32toHost(BitConverter.ToInt32(tmp, 0)) :
							(uint)Utils.LE32toHost(BitConverter.ToInt32(tmp, 0));
						offset = val * 0x200 + 0x4 * ((j >> 3) & 0x1ff);
						inFs.Seek(parent.GetBlkOffset(offset), SeekOrigin.Begin);
						inFs.Read(tmp, 0, sizeof(uint));
						val = isBE ?
							(uint)Utils.BE32toHost(BitConverter.ToInt32(tmp, 0)) :
							(uint)Utils.LE32toHost(BitConverter.ToInt32(tmp, 0));
						offset = val * 0x200 + 0x200 * (j & 0x7);
						inFs.Seek(parent.GetBlkOffset(offset), SeekOrigin.Begin);
						inFs.Read(buf, i * 0x200, 0x200);
					}
				}

				return 0;
			}

			/// <summary>
			/// ファイルツリー構造のInodeクラスオブジェクトを構築
			/// </summary>
			/// <param name="inFs"></param>
			/// <param name="isBE"></param>
			/// <param name="baseInoList"></param>
			/// <param name="actInoList"></param>
			/// <param name="builtIno"></param>
			/// <returns></returns>
			internal int SetupDirInode(in FileStream inFs, bool isBE,
						in List<Inode> baseInoList,
						ref List<uint> actInoList,
						out Inode builtIno)
			{
				DirEntry entry;
				byte[] inoData;
				int ret, offset = 0;
				Inode matchIno, _matchIno;

				builtIno = new Inode(parent)
				{
					nlink = nlink,
					len = len,
					atime = atime,
					mtime = mtime,
					ctime = ctime,
					directDiskBlks = directDiskBlks,
					indirectDiskBlks = indirectDiskBlks,
					flags = flags,
					blocks = blocks,

					inum = inum,
					ino_type = ino_type,
					ino_perm = ino_perm,
					ino_name = ino_name,
					ino_lnktarget = ino_lnktarget,
					buf = null,
				};
				dirSetupDone = true;

				ret = GetInodeData(in inFs, out inoData, isBE);
				if (ret != 0)
					return ret;
				while (offset < inoData.Length)
				{
					entry = new DirEntry();
					offset = entry.ParseDirEntry(in inoData, offset, isBE);
					if (offset < 0)
						break;

					//Console.WriteLine("    {0,4}: type-> 0x{1:x02}, \"{2}\" ({3} chars)",
					//		entry.inum, entry.type, entry.name, entry.namelen);

					actInoList.Add(entry.inum);
					if (inum == entry.inum && entry.name.Equals(".."))
						ino_name = builtIno.ino_name = "/";

					matchIno = baseInoList.Find(x =>
								x.inum == entry.inum &&
								x.ino_type == entry.type << FFSFileInfo.INOFT_OFS);
					if (matchIno == null)
						continue;

					matchIno.ino_name = entry.name;
					_matchIno = null;
					if (entry.type == FFSFileInfo.FT_DIR)
					{
						if (matchIno.dirSetupDone == false)
							matchIno.SetupDirInode(in inFs, isBE, in baseInoList,
									ref actInoList, out _matchIno);
					}
					else
					{
						_matchIno = new Inode(parent)
						{
							nlink = matchIno.nlink,
							len = matchIno.len,
							atime = matchIno.atime,
							mtime = matchIno.mtime,
							ctime = matchIno.ctime,
							directDiskBlks = matchIno.directDiskBlks,
							indirectDiskBlks = matchIno.indirectDiskBlks,
							flags = matchIno.flags,
							blocks = matchIno.blocks,

							inum = matchIno.inum,
							ino_type = matchIno.ino_type,
							ino_perm = matchIno.ino_perm,
							ino_name = matchIno.ino_name,
							ino_lnktarget = matchIno.ino_lnktarget,
							buf = null,
						};
					}

					if (_matchIno == null)
						continue;
					if (builtIno.dirFileEnt == null)
						builtIno.dirFileEnt = new List<Inode>();

					//if (_matchIno.ino_type != FFSFileInfo.INOFT_DIR)
					//	_matchIno.PrintInode();
					builtIno.dirFileEnt.Add(_matchIno);
				}

				//builtIno.PrintInode();
				return 0;
			}

			/// <summary>
			/// Inode情報を表示（デバッグ用）
			/// </summary>
			internal void PrintInode()
			{
				DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				dt = dt.AddSeconds(mtime);
				Console.WriteLine("{0,4}: {1,20}, type: {2:x04}, perm: {3}, mtime: {4}, size: 0x{5:x08} ({5} bytes), blocks: {6}",
						inum,
						ino_name != null ? $"\"{ino_name}\"" : "(null)",
						ino_type,
						Convert.ToString(ino_perm, 8),
						dt.ToString("yyyy/mm/dd HH:mm:ss"),
						len,
						blocks);
				if (ino_type == FFSFileInfo.INOFT_LNK)
					Console.WriteLine("    LINK-> {0}",
							ino_lnktarget);
			}

			/// <summary>
			/// ディレクトリinode内のディレクトリ/ファイルを再帰的に表示
			/// </summary>
			/// <param name="actInoList">参照用実inodeリスト</param>
			/// <param name="path">現在のディレクトリパス</param>
			/// <param name="isBE"></param>
			/// <param name="skipHard">ハードリンクをスキップ</param>
			internal void PrintDirFiles(in StreamWriter sw, in List<uint> actInoList,
						string path, bool isBE, bool skipHard)
			{
				DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				dt = dt.AddSeconds(mtime);
				string attr, uid_gid = string.Format($"{uid}/{gid}");
				string line;

				/* skipHard指定時ハードリンクをスキップ */
				if (skipHard &&
				    ino_type == FFSFileInfo.INOFT_REG &&
					actInoList.FindAll(x => x == inum).Count > 1)
					return;

				/* type */
				if (FFSFileInfo.ftchar.TryGetValue(ino_type, out string tmp))
					attr = tmp;
				else
					attr = "-";

				/* permission */
				for (int i = FFSFileInfo.FP_CL_USR; i >= 0; i--)
				{
					attr += ((ino_perm & FFSFileInfo.FP_CL(FFSFileInfo.FP_READ, i)) > 0) ?
							"r" : "-";
					attr += ((ino_perm & FFSFileInfo.FP_CL(FFSFileInfo.FP_WRTE, i)) > 0) ?
							"w" : "-";
					attr += ((ino_perm & FFSFileInfo.FP_CL(FFSFileInfo.FP_EXEC, i)) > 0) ?
							"x" : "-";
				}
				attr += ((ino_perm & FFSFileInfo.FP_STCK) > 0) ? "t" : "-";

				line = string.Format("{0} {1,-10} {2,10} {3} {4}{5}{6}",
							attr,
							uid_gid,
							len,
							dt.ToString("yyyy-MM-dd HH:mm:ss"),
							path,
							ino_name,
							ino_type == FFSFileInfo.INOFT_LNK ?
								" -> " + ino_lnktarget : "");
				sw.WriteLine(line);

				if (ino_type == FFSFileInfo.INOFT_DIR &&
					dirFileEnt != null)
				{
					path += ino_name + (path != null ? "/" : "");
					foreach (Inode _ino in dirFileEnt)
						_ino.PrintDirFiles(in sw, in actInoList, path, isBE, skipHard);
				}
			}

			/// <summary>
			/// ディレクトリinode内のディレクトリ/ファイルを再帰的に展開
			/// </summary>
			/// <param name="inFs"></param>
			/// <param name="actInoList"></param>
			/// <param name="path"></param>
			/// <param name="outDir"></param>
			/// <param name="isBE"></param>
			/// <param name="skipHard"></param>
			/// <returns></returns>
			internal int ExtractDirFiles(in FileStream inFs, in List<uint> actInoList,
						string path, string outDir, bool isBE, bool skipHard)
			{
				int ret;
				DateTime dt;

				/* skipHard指定時ハードリンクをスキップ */
				if (skipHard &&
					ino_type == FFSFileInfo.INOFT_REG &&
					actInoList.FindAll(x => x == inum).Count > 1)
					return 0;

				dt = Utils.UnixZeroUTC();
				switch (ino_type >> FFSFileInfo.INOFT_OFS)
				{
					/* Directory */
					case FFSFileInfo.FT_DIR:
						string dir = outDir + path + ino_name;
						if (!Directory.Exists(dir))
							Directory.CreateDirectory(dir);
						/*
						 * 当該ディレクトリをExplorerで開いていると例外になる為
						 * それに中にファイルを書くとどのみちmtimeが更新される
						 */
						//Directory.SetCreationTime(dir, dt.AddSeconds(ctime));
						//Directory.SetLastWriteTime(dir, dt.AddSeconds(mtime));
						//Directory.SetLastAccessTime(dir, dt.AddSeconds(atime));

						if (dirFileEnt != null)
						{
							path += ino_name + (path != null ? "/" : "");
							foreach (Inode _ino in dirFileEnt)
								_ino.ExtractDirFiles(in inFs, in actInoList, path,
										outDir, isBE, skipHard);
						}
						break;
					/* Regular File */
					case FFSFileInfo.FT_REG:
						string file = outDir + path + ino_name;
						ret = GetInodeData(in inFs, out byte[] data, isBE);
						if (ret != 0)
							return ret;
						using (FileStream fs = new FileStream(@file, FileMode.Create,
									FileAccess.Write, FileShare.None))
						{
							fs.Write(data, 0, Convert.ToInt32(len));
						}
						File.SetCreationTime(file, dt.AddSeconds(ctime));
						File.SetLastWriteTime(file, dt.AddSeconds(mtime));
						File.SetLastAccessTime(file, dt.AddSeconds(atime));
						break;
				}

				return 0;
			}
		}

		private class DirEntry
		{
			internal uint inum = 0;
			internal ushort entlen = 0;
			internal byte type = 0;
			internal byte namelen = 0;
			internal string name;

			/// <summary>
			/// ディレクトリInodeのデータ領域からエントリをパース
			/// </summary>
			/// <param name="buf">ディレクトリInodeのデータ領域</param>
			/// <param name="offset">データ領域内の開始オフセット</param>
			/// <param name="isBE">FSがBEか否か</param>
			/// <returns></returns>
			internal int ParseDirEntry(in byte[] buf, int offset, bool isBE)
			{
				FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance |
						BindingFlags.NonPublic);
				uint val;

				/*
				 * バッファの残りが最低限の長さを満たさない場合は中止
				 * (inum, entlen, type, namelen, name（最低1文字）
				 */
				if (buf.Length - offset < sizeof(uint) * 2 + sizeof(char))
					return -1;

				foreach (FieldInfo field in fields)
				{
					if (field.IsNotSerialized)
						continue;
					if (field.FieldType == typeof(uint) ||
					    field.FieldType == typeof(ushort))
					{
						val = isBE ?
							(uint)Utils.BE32toHost(BitConverter.ToInt32(buf, offset)) :
							(uint)Utils.LE32toHost(BitConverter.ToInt32(buf, offset));
						if (field.FieldType == typeof(uint))
						{
							field.SetValue(this, val);
							offset += sizeof(uint);
						}
						else
						{
							if (isBE)
								field.SetValue(this, Convert.ToUInt16(val >> 16));
							else
								field.SetValue(this, Convert.ToUInt16(val & ushort.MaxValue));
							offset += sizeof(ushort);
						}
					}
					else if (field.FieldType == typeof(byte))
					{
						field.SetValue(this, buf[offset]);
						offset++;
					}
				}

				if (entlen == 0)
					return -1;

				if (namelen > 0)
				{
					name = Encoding.ASCII.GetString(buf, offset, namelen);
					offset += 4 - (namelen % 4) + namelen;
				}

				return offset;
			}
		}

		private static class FFSFileInfo
		{
			/* File Permissions */
			internal const uint FPERM_MASK = 0xfff;
			internal const uint FP_EXEC = 01;
			internal const uint FP_WRTE = 02;
			internal const uint FP_READ = 04;
			internal const uint FP_STCK = 0x200; //01000;

			internal const int FP_CL_USR = 2;
			internal const int FP_CL_GRP = 1;
			internal const int FP_CL_OTH = 0;
			internal static uint FP_CL(uint _perm, int _class)
				=> _perm << (3 * _class);

			/* File Types */
			internal const uint FT_FIFO = 0x1;
			internal const uint FT_CHR = 0x2;
			internal const uint FT_DIR = 0x4;
			internal const uint FT_BLK = 0x6;
			internal const uint FT_REG = 0x8;
			internal const uint FT_LNK = 0xa;
			internal const uint FT_SOCK = 0xc;
			internal const uint FT_WHT = 0xe;

			internal const uint INOFTYPE_MASK = 0xf000;
			internal const int INOFT_OFS = 12;
			internal static uint INOFT(uint _type) => _type << INOFT_OFS;
			internal static uint INOFT_FIFO = INOFT(FT_FIFO);
			internal static uint INOFT_CHR = INOFT(FT_CHR);
			internal static uint INOFT_DIR = INOFT(FT_DIR);
			internal static uint INOFT_BLK = INOFT(FT_BLK);
			internal static uint INOFT_REG = INOFT(FT_REG);
			internal static uint INOFT_LNK = INOFT(FT_LNK);
			internal static uint INOFT_SOCK = INOFT(FT_SOCK);
			internal static uint INOFT_WHT = INOFT(FT_WHT);

			/// <summary>
			/// リストにおけるファイル属性表示用
			/// </summary>
			internal static Dictionary<uint, string> ftchar
					= new Dictionary<uint, string>()
			{
				{ INOFT_FIFO, "p" },
				{ INOFT_CHR, "c" },
				{ INOFT_DIR, "d" },
				{ INOFT_BLK, "b" },
				{ INOFT_LNK, "l" },
				{ INOFT_SOCK, "s" },
			};
		}
	}
}
