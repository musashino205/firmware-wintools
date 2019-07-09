namespace firmware_wintools
{
	class ArgMap
	{
		public static int Set_StrParamFromArgs(string[] args, int index, ref string target)
		{
			if (index + 1 < args.Length && !args[index + 1].StartsWith("-"))
			{
				target = args[index + 1];
				return 0;
			}
			else
			{
				return 1;
			}
		}

		public void Init_args(string[] args, ref Program.Properties props)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i].Replace("-", ""))
					{
						case "i":
							if (Set_StrParamFromArgs(args, i, ref props.inFile) == 0)
								i++;
							break;
						case "o":
							if (Set_StrParamFromArgs(args, i, ref props.outFile) == 0)
								i++;
							break;
						case "h":
							props.help = true;
							break;
						case "D":
							props.debug = true;
							break;
						case "":    // ハイフンのみ ('-') 対策
							props.prop_invalid = true;
							break;
					}
					props.propcnt++;
				}
			}
		}
	}
}
