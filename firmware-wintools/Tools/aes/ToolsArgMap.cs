namespace firmware_wintools.Tools
{
	internal partial class Aes
	{
		/// <summary>
		/// コマンドライン引数 (<paramref name="args"/>) を解析し、nec-encの機能プロパティを取得します
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">nec-encの機能プロパティ</param>
		public static void
		Init_args(string[] args, int arg_idx, ref Properties subprops)
		{
			for (int i = arg_idx; i < args.Length; i++)
			{
				if (!args[i].StartsWith("-"))
					continue;

				switch (args[i].Replace("-", ""))
				{
					case "d":	// decryption mode
						subprops.decrypt = true;
						break;
					case "K":	// aes key (hex)
						subprops.hex_key = true;
						goto case "k";
					case "k":	// aes key (text)
						if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.key) == 0)
							i++;
						break;
					case "l":	// length
						if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.len) == 0)
							i++;
						break;
					case "O":	// offset
						string offset = null;
						if (ArgMap.Set_StrParamFromArgs(args, i, ref offset) == 0 &&
							Utils.StrToInt(offset, out int conv_offset,
								System.Globalization.NumberStyles.None))
						{
							subprops.offset = conv_offset;
							i++;
						}
						break;
					case "s":	// key length (short, 128)
						subprops.keylen = 128;
						break;
					case "V":	// aes iv (hex)
						subprops.hex_iv = true;
						goto case "v";
					case "v":	// aes iv (text)
						if (ArgMap.Set_StrParamFromArgs(args, i, ref subprops.iv) == 0)
							i++;
						break;
				}
			}
		}
	}
}
