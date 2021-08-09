using System;
using System.IO;

namespace firmware_wintools.Tools
{
	static class BinCut
	{
		public struct Properties
		{
			public long len;
			public long offset;
			public long pad;
			public long padBS;
		}

		private static void PrintHelp(int arg_idx)
		{
			Console.WriteLine(Lang.Tools.BinCutRes.Help_Usage +
				Lang.Tools.BinCutRes.FuncDesc +
				Environment.NewLine,
				arg_idx < 2 ? "" : "firmware-wintools ");	// 引数インデックスが2未満（symlink呼び出し）の場合機能名のみ
			// 共通オプション表示
			Program.PrintCommonOption();
			// 機能オプション表示
			Console.WriteLine(Lang.CommonRes.Help_FunctionOpts +
				Lang.Tools.BinCutRes.Help_Options_Length +
				Lang.Tools.BinCutRes.Help_Options_Offset +
				Lang.Tools.BinCutRes.Help_Options_Pad +
				Lang.Tools.BinCutRes.Help_Options_PadBS);
		}

		private static void PrintInfo(Properties subprops, long data_len)
		{
			Console.WriteLine(Lang.Tools.BinCutRes.Info);
			Console.WriteLine(Lang.Tools.BinCutRes.Info_length,
				data_len);
			Console.WriteLine(Lang.Tools.BinCutRes.Info_offset,
				subprops.offset);
			if (subprops.pad > 0)
				Console.WriteLine(Lang.Tools.BinCutRes.Info_Pad,
					subprops.pad);
			if (subprops.padBS > 0)
				Console.WriteLine(Lang.Tools.BinCutRes.Info_PadBS,
					subprops.padBS);
		}

		public static int Do_BinCut(string[] args, int arg_idx, Program.Properties props)
		{
			Properties subprops = new Properties();

			Firmware fw = new Firmware();

			if (props.help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ToolsArgMap.Init_args_BinCut(args, arg_idx, ref subprops);

			fw.inFInfo = new FileInfo(props.inFile);

			/* check offset/length */
			if (subprops.offset > fw.inFInfo.Length)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.BinCutRes.Warning_LargeOffset);
				subprops.offset = 0;
			}

			if (subprops.len < 0 || subprops.len > fw.inFInfo.Length - subprops.offset)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Warning_Prefix +
					Lang.Tools.BinCutRes.Warning_InvalidLength);
				subprops.len = 0;
			}

			if (subprops.pad > 0 && subprops.padBS > 0)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BinCutRes.Error_DualPad);
				return 1;
			}

			if (subprops.pad != 0 &&
				subprops.pad <
					(subprops.len != 0 ? subprops.len : fw.inFInfo.Length - subprops.offset))
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BinCutRes.Error_SmallPadSize);
				return 1;
			}

			if (subprops.padBS < 0)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.BinCutRes.Error_InvalidPadBSSize);
				return 1;
			}
			/* check offset/length/pad/pad_with_bs end */

			long data_len =
				subprops.len != 0 ? subprops.len : (fw.inFInfo.Length - subprops.offset);
			if (!props.quiet)
				PrintInfo(subprops, data_len);

			try
			{
				using (fw.inFs = new FileStream(props.inFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				using (fw.outFs = new FileStream(props.outFile, FileMode.Create,
							FileAccess.Write, FileShare.None))
				{
					fw.inFs.Seek(subprops.offset, SeekOrigin.Begin);

					fw.inFs.CopyTo(fw.outFs);

					if (subprops.len != 0)
						fw.outFs.SetLength(subprops.len);

					/* padding無しであればここで終了 */
					if (subprops.pad == 0 && subprops.padBS == 0)
						return 0;

					long padded_len;
					/*
					 * blocksizeでのpaddingの際は余りを確認して、あるようならblocksizeを足す
					 * 指定サイズへのpaddingの際はそのサイズをそのまま指定
					 */
					if (subprops.padBS > 0)
						padded_len = subprops.padBS *
							((data_len / subprops.padBS) + (data_len % subprops.padBS > 0 ? 1 : 0));
					else
						padded_len = subprops.pad;
					long len;
					byte[] buf = new byte[0x10000];

					for (len = padded_len - data_len; len >= buf.Length; len -= buf.Length)
					{
						fw.outFs.Write(buf, 0, buf.Length);
					}

					if (len > 0)
						fw.outFs.Write(buf, 0, (int)len);
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
