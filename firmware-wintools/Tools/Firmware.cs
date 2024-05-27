using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace firmware_wintools.Tools
{
	class HeaderFooter
	{
		[NonSerialized]
		internal byte[] Data = null;
		[NonSerialized]
		internal long totalLen = 0;

		internal int LoadData(in FileStream fs, int length)
		{
			/* 0 or less, or too long */
			if (length < 0 || length > 0x400)
				return -22;

			Data = new byte[length];
			return fs.Read(Data, 0, length) != length ? -22 : 0;
		}

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

		internal int DeserializeProps(int index)
		{
			FieldInfo[] fields;

			if (Data == null || Data.Length == 0)
				return -1;

			fields = GetType().GetFields(
					BindingFlags.Instance | BindingFlags.DeclaredOnly |
					BindingFlags.Public | BindingFlags.NonPublic);
			if (fields == null)
				return -1;

			foreach (FieldInfo f in fields)
			{
				if (f.IsNotSerialized)
					continue;

				switch (f.GetValue(this))
				{
					case byte byteVal when (Data.Length - index) > 0:
						byteVal = Data[index];
						f.SetValue(this, byteVal);
						index++;
						break;

					case ushort ushortVal when (Data.Length - index) >= sizeof(ushort):
						ushortVal = BitConverter.ToUInt16(Data, index);
						ushortVal = (ushort)IPAddress.NetworkToHostOrder(ushortVal);
						f.SetValue(this, ushortVal);
						index += sizeof(ushort);
						break;

					case int intVal when (Data.Length - index) >= sizeof(int):
						intVal = BitConverter.ToInt32(Data, index);
						intVal = Utils.BE32toHost(intVal);
						f.SetValue(this, intVal);
						index += sizeof(int);
						break;

					case uint uintVal when (Data.Length - index) >= sizeof(uint):
						uintVal = BitConverter.ToUInt32(Data, index);
						uintVal = (uint)Utils.BE32toHost(uintVal);
						f.SetValue(this, uintVal);
						index += sizeof(uint);
						break;

					case long longVal when (Data.Length - index) >= sizeof(long):
						longVal = BitConverter.ToInt64(Data, index);
						longVal = IPAddress.NetworkToHostOrder(longVal);
						f.SetValue(this, longVal);
						index += sizeof(long);
						break;

					case byte[] byteAry
							when byteAry != null
							&& (Data.Length - index) >= byteAry.Length:
						 Buffer.BlockCopy(Data, index, byteAry, 0, byteAry.Length);
						//f.SetValue(this, byteAry);
						index += byteAry.Length;
						break;

					case int[] intAry
							when intAry != null
							&& (Data.Length - index) >= intAry.Length * sizeof(int):
						for (int i = 0; i < intAry.Length; i++)
						{
							intAry[i] = BitConverter.ToInt32(Data, index + i * sizeof(int));
							intAry[i] = Utils.BE32toHost(intAry[i]);
						}
						index += intAry.Length * sizeof(int);
						break;

					case long[] longAry
							when longAry != null
							&& (Data.Length - index) >= longAry.Length * sizeof(long):
						for (int i = 0; i < longAry.Length; i++)
						{
							longAry[i] = BitConverter.ToInt64(Data, index + i * sizeof(long));
							longAry[i] = Utils.BE64toHost(longAry[i]);
						}
						index += longAry.Length * sizeof(long);
						break;

					default:
						continue;
				}
			}

			return index;
		}

		internal int DeserializeProps()
			=> DeserializeProps(0);
	}

	class Firmware
	{
		public FileInfo inFInfo;
		public string outFile;
		public FileStream inFs = null;
		public FileStream outFs = null;
		public FileMode outFMode;
		public byte[] headerData;
		public byte[] data;
		public byte[] footerData;
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

			if (!dataonly && headerData != null && headerData.LongLength > 0)
			{
				if (BytesToFile(ref headerData, ref outFs) != headerData.LongLength)
					return 1;
			}

			if (BytesToFile(ref data, ref outFs) != data.LongLength)
				return 1;

			if (!dataonly && footerData != null && footerData.LongLength > 0)
				if (BytesToFile(ref footerData, ref outFs) != footerData.LongLength)
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
