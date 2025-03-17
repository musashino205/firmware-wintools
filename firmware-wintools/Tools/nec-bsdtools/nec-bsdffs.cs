namespace firmware_wintools.Tools
{
	internal class Nec_BsdFFS : Tool
	{
		/* ツール情報　*/
		public override string name => "nec-bsdffs";
		public override string desc => "find and list/extract directories/files from NEC NetBSD FFS";
		public override string descFmt { get => "    {0}		: {1}"; }
		public override bool skipOFChk => true;

		/// <summary>
		/// nec-bsdffsメイン関数
		/// <para>Programのツールリストからbsdffsのインスタンスを取得し、そのまま Do() に飛ぶ</para>
		/// </summary>
		/// <param name="args"></param>
		/// <param name="arg_idx"></param>
		/// <param name="props"></param>
		/// <returns></returns>
		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			Tool tool = Program.toolList.Find(x => x.name == "bsdffs");

			return (tool != null) ? tool.Do(args, arg_idx, props) : -1;
		}
	}
}
