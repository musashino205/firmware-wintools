using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace firmware_wintools
{
	class ArgMap
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

		public void Init_args(string[] args, ref Program.Properties props)
		{
			for (int i = 0; i < args.Length; i++)
			{
				string param = args[i];

				if (param.StartsWith("-"))
				{
					switch (param.Replace("-", ""))
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
					}
				}
			}
		}
	}
}
