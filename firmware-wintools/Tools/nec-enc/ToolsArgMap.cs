using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace firmware_wintools.Tools
{
	class ToolsArgMap
	{
		private int Set_StrParamFromArgs(string[] args, int index, ref string target)
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

		public void Init_args_NecEnc(string[] args, ref Tools.nec_enc.Properties props)
		{
			for (int i = 0; i < args.Length; i++)
			{
				string param = args[i];

				if (param.StartsWith("-"))
				{
					switch (param.Replace("-", ""))
					{
						case "k":
							if (Set_StrParamFromArgs(args, i, ref props.key) == 0)
								i++;
							break;
					}
				}
			}
		}
	}
}
