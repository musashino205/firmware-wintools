/*
 * BSD FFS (Fast File System)
 *
 * super block: struct fs (fs.h)
 * inode: struct ufs1_dinode (dinode.h)
 *
 * https://ja.osdn.net/projects/ffsdrv/releases/
 */

using System;
using System.Reflection;

namespace firmware_wintools.Tools
{
	internal partial class Nec_BsdFFS
	{
		private class DiskSuperBlk
		{
			/* 0x0-0x7: unused */
			internal int superBlkCnt = 0;
			internal int cylBlkCnt = 0;
			internal int inodeBlkCnt = 0;
			internal int dataBlkCnt = 0;
			/* 0x18-0x1f: unused */
			internal int unixtime = 0;
			/* 0x24-0x55b: unused */
			internal uint magic = 0;

			[NonSerialized]
			internal static readonly int SUPERBLK_LEN = 0x560;
			[NonSerialized]
			internal static readonly uint SUPERBLK_BE_MAGIC = 0x00011954;
			[NonSerialized]
			internal static readonly uint SUPERBLK_LE_MAGIC = 0x54190100;
			[NonSerialized]
			internal bool isBE = true;
			[NonSerialized]
			internal byte[] buf = new byte[SUPERBLK_LEN];

			internal int CheckSuperBlk()
			{
				int i = 0;
				uint val;
				FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance |
									BindingFlags.NonPublic);

				foreach (var field in fields)
				{
					if (field.IsNotSerialized)
						continue;
					if (field.Name == "superBlkCnt" ||
					    field.Name == "unixtime")
						i += sizeof(uint) * 2;
					if (field.Name == "magic")
						i += sizeof(uint) * 334;

					val = (uint)Utils.BE32toHost(BitConverter.ToInt32(buf, i));
					if (field.FieldType == typeof(int))
						field.SetValue(this, (int)val);
					else
						field.SetValue(this, val);
					i += sizeof(uint);
				}

				if (magic != SUPERBLK_BE_MAGIC &&
					magic != SUPERBLK_LE_MAGIC)
					return -1;

				if (magic == SUPERBLK_LE_MAGIC)
				{
					isBE = false;
					foreach (var field in fields)
					{
						var _val = field.GetValue(this);

						if (field.Name == "magic")
							continue;

						if (field.IsNotSerialized)
							continue;

						val = (uint)Utils.BE32toHost((int)_val);
						if (field.FieldType == typeof(int))
							field.SetValue(this, (int)val);
						else
							field.SetValue(this, val);
					}
				}

				if (inodeBlkCnt > ushort.MaxValue ||
					dataBlkCnt > ushort.MaxValue)
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							"block num of inode or data is too large (inode: {0}, data: {1})",
							inodeBlkCnt, dataBlkCnt);
					return -2;
				}

				return 0;
			}

			internal void PrintSuperBlk(long curoff)
			{
				DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				dt = dt.AddSeconds(unixtime);

				Console.Error.WriteLine("--- Super Block Info ---");
				Console.Error.WriteLine("1st SuperBlock Offset: 0x{0:X}", curoff);
				Console.Error.WriteLine("Magic                : 0x{0:X08} (endian: {1})",
						magic, isBE ? "Big" : "Little");
				Console.Error.WriteLine("Super Blocks         : {0}", superBlkCnt);
				Console.Error.WriteLine("Cylinder Blocks      : {0}", cylBlkCnt);
				Console.Error.WriteLine("inode Blocks         : {0}", inodeBlkCnt);
				Console.Error.WriteLine("Data Blocks          : {0}", dataBlkCnt);
				Console.Error.WriteLine("Last Written         : {0}", dt.ToString("yyyy/MM/dd HH:mm:ss"));
				Console.Error.WriteLine();
			}
		}
	}
}
