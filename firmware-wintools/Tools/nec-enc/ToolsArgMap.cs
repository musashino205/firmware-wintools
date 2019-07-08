namespace firmware_wintools.Tools
{
	partial class ToolsArgMap
	{
		public void Init_args_NecEnc(string[] args, ref Tools.Nec_Enc.Properties props)
		{
			for (int i = 0; i < args.Length; i++)
			{
				string param = args[i];

				if (param.StartsWith("-"))
				{
					switch (param.Replace("-", ""))
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
