namespace firmware_wintools.Tools
{
	class NecEncFirmware : Firmware
	{
		internal byte[] buf_ptn;

		byte ptn = 1;

		const int PTN_LEN = 251;

		internal void GenerateBasePattern(int length)
		{
			for (int i = 0; i < length; i++)
			{
				buf_ptn[i] = ptn;
				ptn++;

				if (ptn > PTN_LEN)
					ptn = 1;
			}
		}
	}
}
