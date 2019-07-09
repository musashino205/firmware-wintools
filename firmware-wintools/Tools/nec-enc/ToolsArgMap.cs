namespace firmware_wintools.Tools
{
	partial class ToolsArgMap
	{
		public void Init_args_NecEnc(string[] args, ref Tools.Nec_Enc.Properties props)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i].Replace("-", ""))
					{
						case "k":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref props.key) == 0)
								i++;
							break;
					}
				}
			}
		}
	}
}
