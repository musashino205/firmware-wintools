using System;
using System.Net;

namespace firmware_wintools.Tools
{
	class EdimaxHeader : HeaderFooter
	{
		internal byte[] sign = new byte[4];
		internal int start;
		internal int flash;
		internal byte[] model = new byte[4];
		internal int size;
	}

	class EdimaxFooter: HeaderFooter
	{
		internal ushort cksum;
	}

	class EdimaxFirmware : Firmware
	{
		internal EdimaxHeader header = new();
		internal EdimaxFooter footer = new();

		internal int dataLen;

		/// <summary>
		/// <paramref name="buf"/> からデータ末尾に付加するchecksumの算出を行います
		/// </summary>
		/// <param name="buf">checksum算出対象データ</param>
		/// <param name="isbe">BEでの算出</param>
		/// <returns></returns>
		internal ushort CalcCksum(bool isbe)
		{
			ushort cksum = 0;

			for (int i = 0; i < data.Length / 2; i++)
			{
				cksum -= isbe ?
						(ushort)IPAddress.HostToNetworkOrder(
								BitConverter.ToInt16(data, i * 2)) :
						BitConverter.ToUInt16(data, i);
			}

			return cksum;
		}
	}
}
