namespace firmware_wintools.Tools
{
	partial class ToolsArgMap
	{
		public void Init_args_Xorimage(string[] args, ref Tools.XorImage.Properties props)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i].Replace("-", ""))
					{
						case "p":
							if (ArgMap.Set_StrParamFromArgs(args, i, ref props.pattern) == 0)
								i++;
							break;
						case "x":
							props.ishex = true;
							break;
					}
				}
			}
		}
	}
}
