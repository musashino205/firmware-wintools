namespace firmware_wintools.Tools
{
	public abstract class Tool
	{
		public string _name = "";
		public virtual string name { get => ""; }
		public virtual string desc { get => ""; }
		public virtual string descFmt { get => ""; }

		internal abstract int
		Do(string[] args, int arg_idx, Program.Properties baseProps);
	}
}
