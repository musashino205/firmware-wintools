using System;
using System.Collections.Generic;
using System.IO;

namespace firmware_wintools.Tools
{
	internal class BinCut : Tool
	{
		/* ツール情報　*/
		public override string name { get => "bincut"; }
		public override string desc { get => Lang.Tools.BinCutRes.FuncDesc; }
		public override string descFmt { get => Lang.Tools.BinCutRes.Main_FuncDesc_Fmt; }
		public override string resName => "BinCutRes";

		private long Length = -1;
		private long Offset = 0;
		private long Pad = 0;
		private long PadBS = 0;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 'l', PType = Param.PTYPE.LONG, SetField = "Length", HelpKey = "Help_Options_Length" },
			new Param() { PChar = 'O', PType = Param.PTYPE.LONG, SetField = "Offset", HelpKey = "Help_Options_Offset" },
			new Param() { PChar = 'p', PType = Param.PTYPE.LONG, SetField = "Pad", HelpKey = "Help_Options_Pad" },
			new Param() { PChar = 'P', PType = Param.PTYPE.LONG, SetField = "PadBS", HelpKey = "Help_Options_PadBS" }
		};

		private void PrintInfo()
		{
			Console.WriteLine(Lang.Tools.BinCutRes.Info);
			Console.WriteLine(Lang.Tools.BinCutRes.Info_length,
				Length);
			Console.WriteLine(Lang.Tools.BinCutRes.Info_offset,
				Offset);
			if (Pad > 0)
				Console.WriteLine(Lang.Tools.BinCutRes.Info_Pad,
					Pad);
			if (PadBS > 0)
				Console.WriteLine(Lang.Tools.BinCutRes.Info_PadBS,
					PadBS);
		}

		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			Firmware fw = new Firmware();

			if (props.Help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			InitArgs(args, arg_idx);

			fw.inFInfo = new FileInfo(props.InFile);

			/* check offset/length */
			if (Offset >  fw.inFInfo.Length)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.BinCutRes.Warning_LargeOffset);
				Offset = 0;
			}

			if (Length == -1)
				Length = fw.inFInfo.Length - Offset;
			if (Length < 0 ||
			    Length > fw.inFInfo.Length - Offset)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.BinCutRes.Warning_InvalidLength);
				Length = fw.inFInfo.Length - Offset;
			}

			if (Pad > 0 && PadBS > 0)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BinCutRes.Error_DualPad);
				return 1;
			}

			if (Pad > 0 && Pad < Length)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BinCutRes.Error_SmallPadSize);
				return 1;
			}

			if (PadBS < 0)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BinCutRes.Error_InvalidPadBSSize);
				return 1;
			}
			/* check offset/length/pad/pad_with_bs end */

			if (!props.Quiet)
				PrintInfo();

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				using (fw.outFs = new FileStream(props.OutFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
				{
					fw.inFs.Seek(Offset, SeekOrigin.Begin);

					fw.inFs.CopyTo(fw.outFs);

					if (Length != 0)
						fw.outFs.SetLength(Length);

					/* padding無しであればここで終了 */
					if (Pad == 0 && PadBS == 0)
						return 0;

					long padlen;
					/*
					 * blocksizeでのpaddingの際は余りを確認して、あるようならblocksizeを足す
					 * 指定サイズへのpaddingの際はそのサイズをそのまま指定
					 */
					if (PadBS > 0)
						padlen = PadBS *
							((Length / PadBS) + (Length % PadBS > 0 ? 1 : 0));
					else
						padlen = Pad;

					byte[] buf = new byte[0x10000];

					padlen -= Length;
					for (; padlen >= buf.Length; padlen -= buf.Length)
						fw.outFs.Write(buf, 0, buf.Length);

					if (padlen > 0)
						fw.outFs.Write(buf, 0, (int)padlen);
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			return 0;
		}
	}
}
