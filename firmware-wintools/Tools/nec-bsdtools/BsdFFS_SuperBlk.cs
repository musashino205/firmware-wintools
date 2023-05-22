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

namespace firmware_wintools.Tools
{
	internal partial class Nec_BsdFFS
	{
		private class DiskSuperBlk
		{
			internal int _iBlkOffs => _props.Find(x => x.name == "iblkno").value;
			internal int _dBlkOffs => _props.Find(x => x.name == "dblkno").value;
			internal int _magic => _props.Find(x => x.name == "magic").value;
			internal int _totalBlks => _props.Find(x => x.name == "old_size").value;

			internal static readonly int BLKSZ = 0x200;
			internal static readonly int SUPERBLK_LEN = 0x560;
			internal static readonly uint SUPERBLK_BE_MAGIC = 0x00011954;
			internal static readonly uint SUPERBLK_LE_MAGIC = 0x54190100;
			internal bool isBE = true;
			internal byte[] buf = new byte[SUPERBLK_LEN];
			internal static readonly List<Property> _props = new List<Property>()
			{
				new Property(){ name = "magic", desc = "Magic Number", fmt = "{0,-21}: 0x{1:X08} (endian: {2})", type = Property.Type.MGC, offset = 0x55c },
				new Property(){ name = "sblkno", desc = "2nd SuperBlock Offset", fmt = "{0,-21}: 0x{1:X}", type = Property.Type.BLK, offset = 0x8 },
				new Property(){ name = "cblkno", desc = "Cylinder Block Offset", fmt = "{0,-21}: 0x{1:X}", type = Property.Type.BLK, offset = 0xc },
				new Property(){ name = "iblkno", desc = "Inode Block Offset", fmt = "{0,-21}: 0x{1:X}", type = Property.Type.BLK, offset = 0x10 },
				new Property(){ name = "dblkno", desc = "Data Block Offset", fmt = "{0,-21}: 0x{1:X}", type = Property.Type.BLK, offset = 0x14 },
				new Property(){ name = "old_time", desc = "Last Written", fmt = "{0,-21}: {1}", type = Property.Type.DT, offset = 0x20 },
				new Property(){ name = "old_size", desc = "Total Blocks", fmt = "{0,-21}: {1:N0}", offset = 0x24 },
			};

			internal int CheckSuperBlk()
			{
				foreach (Property p in _props)
					p.value = Utils.BE32toHost(BitConverter.ToInt32(buf, p.offset));

				if (_magic != SUPERBLK_BE_MAGIC &&
				    _magic != SUPERBLK_LE_MAGIC)
					return -1;

				if (_magic == SUPERBLK_LE_MAGIC)
				{
					isBE = false;

					foreach (Property p in _props)
					{
						if (p.name == "magic")
							continue;

						p.value = Utils.BE32toHost(p.value);
					}
				}

				/*
				 * inode block offsetまたはdata block offsetが無効な値
				 *
				 * 例: Magic Numberと同じ値が偶然引っかかる位置に存在するが、
				 * 　  当該部分のデータは実際にはヘッダではない
				 */
				if (_iBlkOffs > ushort.MaxValue || _iBlkOffs < 0 ||
					_dBlkOffs > ushort.MaxValue || _dBlkOffs < 0)
					return -1;

				return 0;
			}

			internal void PrintSuperBlk(long curoff)
			{
				Console.Error.WriteLine("--- Super Block Info ---");
				Console.Error.WriteLine("1st SuperBlock Offset: 0x{0:X}", curoff);

				foreach (Property p in _props)
					if (p.type == Property.Type.MGC)
						Console.Error.WriteLine(p.fmt, p.desc, p.value, isBE ? "Big" : "Little");
					else if (p.type == Property.Type.DT)
						Console.Error.WriteLine(p.fmt, p.desc,
							Utils.UnixToUTC(Convert.ToUInt32(p.value)).ToString("yyyy/MM/dd HH:mm:ss"));
					else
						Console.Error.WriteLine(p.fmt, p.desc,
							p.value * (p.type == Property.Type.BLK ? BLKSZ : 1));
				Console.Error.WriteLine();
			}
		}

		private class Property
		{
			internal string name;
			internal string desc;
			internal string fmt;
			internal Type type;
			internal int offset;
			internal int value;

			internal enum Type {
				MGC = 0,
				BLK,
				DT,
			};
		}
	}
}
