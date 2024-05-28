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
using System.Reflection;

namespace firmware_wintools.Tools
{
	internal partial class Nec_BsdFFS
	{
		private class SuperBlock : HeaderFooter
		{
			internal int first = 0;		// 0x0
			internal int unused = 0;
			internal int sblkno = 0;
			internal int cblkno = 0;
			internal int iblkno = 0;	// 0x10
			internal int dblkno = 0;
			internal int cgoffset = 0;
			internal int cgmask = 0;
			internal int time = 0;		// 0x20
			internal int size = 0;
			internal int dsize = 0;
			internal int ncg = 0;
			internal int bsize = 0;		// 0x30
			internal int fsize = 0;
			internal int frag = 0;
			internal int minfree = 0;
			internal int rotdelay = 0;	// 0x40
			internal int rps = 0;
			internal int bmask = 0;
			internal int fmask = 0;
			internal int bshift = 0;	// 0x50
			internal int fshift = 0;
			internal int maxconfig = 0;
			internal int maxbpg = 0;
			internal int fragshift = 0;	// 0x60
			internal int fsbtodb = 0;
			internal int sbsize = 0;
			internal int csmask = 0;
			internal int csshift = 0;	// 0x70
			internal int nindir = 0;
			internal int inopb = 0;
			internal int nspf = 0;
			internal int optim = 0;		// 0x80
			internal int npsect_state = 0;
			internal int interleave = 0;
			internal int trackskew = 0;
			internal int[] id = new int[2];	// 0x90
			internal int csaddr = 0;
			internal int cssize = 0;
			internal int cgsize = 0;	// 0xA0
			internal int ntrack = 0;
			internal int nsect = 0;
			internal int spc = 0;
			internal int ncyl = 0;		// 0xB0
			internal int cpg = 0;
			internal int ipg = 0;
			internal int fpg = 0;
			/* csum */
			internal int ndir = 0;		// 0xC0
			internal int nbfree = 0;
			internal int nifree = 0;
			internal int nffree = 0;
			/* csum end */
			internal byte fmod = 0;		// 0xD0
			internal byte clean = 0;
			internal byte ronly = 0;
			internal byte flags = 0;
			/* fs_u11 -> fs_u2 in Linux Kernel */
			internal byte[] fsmnt = new byte[0x1d4];	// 0xD4
			internal byte[] volname = new byte[0x20];    // 0x2A8
			internal long swuid = 0;		// 0x2C8
			internal int pad = 0;        // 0x2D0
			internal int cgrotor = 0;
			internal int[] ocsp = new int[28];	// 0x2D8
			internal int contigdirs = 0;	// 0x348
			internal int csp = 0;
			internal int maxcluster = 0;	// 0x350
			internal int active = 0;
			internal int cpc = 0;
			internal int maxbsize = 0;
			internal long[] sparecon64 = new long[17];   // 0x360
			internal long sblockloc = 0; // 0x3E8
			/* csumtotal */
			internal long ndir64 = 0;    // 0x3F0
			internal long nbfree64 = 0;
			internal long nifree64 = 0;	// 0x400
			internal long nffree64 = 0;
			internal long numclusters = 0;	// 0x410
			internal long[] spare = new long[3];
			/* csumtotal end */
			internal int timesec = 0;	// 0x430
			internal int timeusec = 0;
			internal long size64 = 0;
			internal long dsize64 = 0;	// 0x440
			internal long csaddr64 = 0;
			internal long pendingblocks = 0;	// 0x450
			internal int pnedinginodes = 0;
			/* fs_u11 -> fs_u2 end */
			internal int[] snapinum = new int[20];   // 0x45C
			internal int avgfilesize = 0;    // 0x4AC
			internal int avgfpdir = 0;		// 0x4B0
			internal int save_cgsize = 0;
			internal int[] sparecon32 = new int[26];   // 0x4B8
			internal int flags2 = 0;		// 0x520
			internal int contigsumsize = 0;
			internal int maxsymlinklen = 0;
			internal int inodefmt = 0;
			internal long maxfilesize = 0;	// 0x530
			internal long qbmask = 0;
			internal long qfmask = 0;	// 0x540
			internal int state = 0;
			internal int postblformat = 0;
			internal int nrpos = 0;		// 0x550
			internal int postbloff = 0;
			internal int rotbloff = 0;
			internal uint magic = 0;      // 0x55C

			[NonSerialized]
			internal const int BLKSZ = 0x200;
			[NonSerialized]
			internal const int LENGTH = 0x560;
			[NonSerialized]
			internal const int MAGIC_OFFSET = 0x55c;
			[NonSerialized]
			internal const uint MAGIC_BE = 0x00011954;
			[NonSerialized]
			internal const uint MAGIC_LE = 0x54190100;
			[NonSerialized]
			internal uint orig_magic = 0;
			[NonSerialized]
			internal Endian endian = Endian.INVAL;
			[NonSerialized]
			internal static readonly List<Property> _props = new List<Property>()
			{
				new Property(){ name = "magic", desc = "Magic Number", fmt = "0x{0:X08} (endian: {1})", type = Property.Type.MGC },
				new Property(){ name = "sblkno", desc = "2nd SuperBlock Offset", fmt = "0x{0:X}", type = Property.Type.BLK },
				new Property(){ name = "cblkno", desc = "Cylinder Block Offset", fmt = "0x{0:X}", type = Property.Type.BLK },
				new Property(){ name = "iblkno", desc = "Inode Block Offset", fmt = "0x{0:X}", type = Property.Type.BLK },
				new Property(){ name = "dblkno", desc = "Data Block Offset", fmt = "0x{0:X}", type = Property.Type.BLK },
				new Property(){ name = "size", desc = "Total Blocks", fmt = "{0:N0}", type = Property.Type.RAW },
				new Property(){ name = "time", desc = "Last Written", fmt = "{0}", type = Property.Type.DT },
				new Property(){ name = "ndir", desc = "Directories", fmt = "{0:N0}", type = Property.Type.RAW },
			};

			internal SuperBlock()
			{
				Data = new byte[LENGTH];
			}

			internal Endian GetEndian()
			{
				uint magic;

				if (Data == null)
					return Endian.INVAL;

				magic = (uint)Utils.BE32toHost(
						BitConverter.ToInt32(Data, MAGIC_OFFSET));
				switch (magic) {
					case MAGIC_BE:
						endian = Endian.BE;
						break;
					case MAGIC_LE:
						endian = Endian.LE;
						break;
					default:
						endian = Endian.INVAL;
						break;
				}

				orig_magic = magic;
				return endian;
			}

			internal bool Evaluate()
			{
				/*
				 * inode block offsetまたはdata block offsetが無効な値
				 *
				 * 例: Magic Numberと同じ値が偶然引っかかる位置に存在するが、
				 * 　  当該部分のデータは実際にはヘッダではない
				 */
				if (iblkno > ushort.MaxValue || iblkno < 0 ||
				    dblkno > ushort.MaxValue || dblkno < 0)
					return false;

				if (ntrack == 0 && nsect == spc)
					return true;
				/* tracks per cylinder * sectors per track = sectors per cylinder */
				if (ntrack * nsect == spc)
					return true;

				return false;
			}

			internal void PrintSuperBlock(long curoff)
			{
				FieldInfo f;
				string tmp;

				Console.Error.WriteLine("--- Super Block Info ---");
				Console.Error.WriteLine("1st SuperBlock Offset: 0x{0:X}", curoff);
				foreach (Property p in _props)
				{
					if ((f = GetType().GetField(p.name,
								BindingFlags.DeclaredOnly |
								BindingFlags.Instance |
								BindingFlags.NonPublic)) == null)
						continue;
					switch (p.type)
					{
						case Property.Type.MGC:
							tmp = string.Format(p.fmt,
										f.GetValue(this),
										endian == Endian.BE ? "Big" : "Little");
							break;
						case Property.Type.DT:
							tmp = Utils.UnixToUTC(Convert.ToUInt32(f.GetValue(this)))
									.ToString("yyyy/MM/dd HH:mm:ss");
							break;
						default:
							long val = (int)f.GetValue(this);
							if (p.type == Property.Type.BLK)
								val = val * BLKSZ + curoff - 0x2000;
							tmp = string.Format(p.fmt, val);
							break;
					}
					Console.Error.WriteLine("{0,-21}: {1}", p.desc, tmp);
				}
				Console.Error.WriteLine();
			}

			internal class Property
			{
				internal string name;
				internal string desc;
				internal string fmt;
				internal Type type;

				internal enum Type
				{
					RAW = 0,
					MGC,
					BLK,
					DT,
				};
			}
		}
	}
}
