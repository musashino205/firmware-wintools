using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace firmware_wintools.Tools
{
	class HeaderFooter
	{
		[NonSerialized]
		internal long totalLen = 0;

		internal long SerializeProps(ref byte[] buf, long index)
		{
			long curLen = index;
			byte[] tmp;
			FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance |
									BindingFlags.NonPublic |
									BindingFlags.Public);

			if (buf == null || buf.Length == 0)
				return -1;

			if (fields.Length == 0)
				return -1;

			foreach (var field in fields)
			{
				if (field.IsNotSerialized)
					continue;

				switch (field.GetValue(this))
				{
					case string str:
						tmp = Encoding.ASCII.GetBytes(str);
						if (tmp.Length == 0 || (buf.Length - curLen) < tmp.Length)
							continue;
						Array.Copy(tmp, 0, buf, index, tmp.Length);
						index += tmp.Length;
						break;

					case byte byteVal when (buf.Length - curLen) > 0:
						buf[index] = byteVal;
						index++;
						break;

					case ushort ushortVal when (buf.Length - curLen) >= sizeof(ushort):
						tmp = BitConverter.GetBytes(ushortVal);
						if (tmp.Length == 0)
							continue;
						Array.Copy(tmp, 0, buf, index, tmp.Length);
						index += tmp.Length;
						break;

					case int intVal when (buf.Length - curLen) >= sizeof(int):
						tmp = BitConverter.GetBytes(intVal);
						if (tmp.Length == 0)
							continue;
						Array.Copy(tmp, 0, buf, index, tmp.Length);
						index += tmp.Length;
						break;

					case uint uintVal when (buf.Length - curLen) >= sizeof(uint):
						tmp = BitConverter.GetBytes(uintVal);
						if (tmp.Length == 0)
							continue;
						Array.Copy(tmp, 0, buf, index, tmp.Length);
						index += tmp.Length;
						break;

					case long longVal when (buf.Length - curLen) >= sizeof(long):
						tmp = BitConverter.GetBytes(longVal);
						if (tmp.Length == 0)
							continue;
						Array.Copy(tmp, 0, buf, index, tmp.Length);
						index += tmp.Length;
						break;

					case byte[] byteAry when (buf.Length - curLen) >= byteAry.Length:
						Array.Copy(byteAry, 0, buf, index, byteAry.Length);
						index += byteAry.Length;
						break;
				}

				curLen = index;
			}

			return curLen;
		}
	}

	class Firmware
	{
		public FileInfo inFInfo;
		public string outFile;
		public FileStream inFs = null;
		public FileStream outFs = null;
		public FileMode outFMode;
		public byte[] header;
		public byte[] data;
		public byte[] footer;
		public long totalLen;

		static internal long FileToBytes(in FileStream fs, ref byte[] array, long length)
		{
			long readLen, copyLen = -1, totalLen = 0;
			const int readBlock = 0x10000;
			byte[] buf = new byte[readBlock];

			if (fs == null || array == null)
				return 0;

			while ((readLen = fs.Read(buf, 0, readBlock)) > 0)
			{
				if ((length - totalLen) < readLen)
					copyLen = length - totalLen;

				Array.Copy(buf, 0, array, totalLen, copyLen >= 0 ?
								copyLen : readLen);
				totalLen += copyLen >= 0 ? copyLen : readLen;

				if (copyLen >= 0)
				{
					/* 途中までのコピーの場合fsを現在起点にマイナス方向へseek */
					fs.Seek(-(readLen - copyLen), SeekOrigin.Current);
					break;
				}
			}

			return totalLen;
		}

		static internal long BytesToFile(ref byte[] array, ref FileStream fs)
		{
			int writeLen;
			long leftSize, totalLen = 0;
			const int writeBlock = 0x10000;
			byte[] buf = new byte[writeBlock];

			if (array == null || fs == null)
				return 0;

			for (long i = 0; i < array.LongLength; i += writeBlock)
			{
				leftSize = array.LongLength - i;
				writeLen = leftSize < writeBlock ? (int)leftSize : writeBlock;

				Array.Copy(array, totalLen, buf, 0, writeLen);

				fs.Write(buf, 0, writeLen);
				totalLen += writeLen;
			}

			return totalLen;
		}

		internal int WriteToFile(bool dataonly)
		{
			if (data == null || data.LongLength == 0)
				return 1;

			if (!dataonly && header != null && header.LongLength > 0)
			{
				if (BytesToFile(ref header, ref outFs) != header.LongLength)
					return 1;
			}

			if (BytesToFile(ref data, ref outFs) != data.LongLength)
				return 1;

			if (!dataonly && footer != null && footer.LongLength > 0)
				if (BytesToFile(ref footer, ref outFs) != footer.LongLength)
					return 1;

			return 0;
		}

		internal int OpenAndWriteToFile(bool dataonly)
		{
			int ret;

			try
			{
				using (outFs = new FileStream(outFile, outFMode,
							FileAccess.Write, FileShare.None))
				{
					ret = WriteToFile(dataonly);
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			return ret;
		}
	}
}
