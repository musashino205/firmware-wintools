using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace firmware_wintools.Tools
{
	internal class MkEdimaxImg : Tool
	{
		/* ツール情報　*/
		public override string name { get => "mkedimaximg"; }
		public override string desc { get => Lang.Tools.MkEdimaxImgRes.FuncDesc; }
		public override string descFmt { get => Lang.Tools.MkEdimaxImgRes.Main_FuncDesc_Fmt; }
		public override string resName => "MkEdimaxImgRes";

		private string Signature = null;
		private string Model = null;
		private int Flash = 0x0;
		private int Start = 0x0;
		private bool IsBE = false;

		internal override List<Param> ParamList => new List<Param>()
		{
			new Param() { PChar = 's', PType = Param.PTYPE.STR, SetField = "Signature", HelpKey = "Help_Options_s" },
			new Param() { PChar = 'm', PType = Param.PTYPE.STR, SetField = "Model", HelpKey = "Help_Options_m" },
			new Param() { PChar = 'f', PType = Param.PTYPE.INT, SetField = "Flash", HelpKey = "Help_Options_f" },
			new Param() { PChar = 'S', PType = Param.PTYPE.INT, SetField = "Start", HelpKey = "Help_Options_s2" },
			new Param() { PChar = 'b', PType = Param.PTYPE.BOOL, SetField = "IsBE", HelpKey = "Help_Options_b" }
		};

		/// <summary>
		/// mkedimaximgの実行情報を表示します
		/// </summary>
		/// <param name="props"></param>
		private void PrintInfo()
		{
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info);
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info_Signature, Signature);
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info_Model, Model);
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info_FlashAddr, Flash);
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info_StartAddr, Start);
			Console.WriteLine(Lang.Tools.MkEdimaxImgRes.Info_BE, IsBE.ToString());
		}

		/// <summary>
		/// mkedimaximgメイン関数
		/// <para>コマンドライン引数とメインプロパティから、edimaxヘッダと checksum
		/// の付加を行います</para>
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="props">メインプロパティ</param>
		/// <returns></returns>
		internal override int Do(string[] args, int arg_idx, Program.Properties props)
		{
			EdimaxFirmware fw = new EdimaxFirmware();
			int ret;

			if (props.Help)
			{
				PrintHelp(arg_idx);
				return 0;
			}

			ret = InitArgs(args, arg_idx);
			if (ret != 0)
				return ret;

			fw.inFInfo = new FileInfo(props.InFile);
			fw.outFile = props.OutFile;
			fw.outFMode = FileMode.Create;

			if (Signature == null)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_NoSignature);
				return 1;
			}
			else if (Signature.Length != 4)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_InvalidSignatureLen);
				return 1;
			}

			if (Model == null)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_NoModel);
				return 1;
			}
			else if (Model.Length != 4)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_InvalidModelLen);
				return 1;
			}

			if (Flash == 0)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_NoInvalidFlash);
				return 1;
			}

			if (Start == 0)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_NoInvalidStart);
				return 1;
			}

			if (fw.inFInfo.Length > int.MaxValue)
			{
				Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
					Lang.Tools.MkEdimaxImgRes.Error_LargeInFile);
				return 1;
			}

			if (!props.Quiet)
				PrintInfo();

			fw.dataLen = Convert.ToInt32(fw.inFInfo.Length + sizeof(short));

			/* ヘッダ設定 */
			fw.header.sign = Encoding.ASCII.GetBytes(Signature);
			fw.header.start = IsBE ?
						IPAddress.HostToNetworkOrder(Start) :
						Start;
			fw.header.flash = IsBE ?
						IPAddress.HostToNetworkOrder(Flash) :
						Flash;
			fw.header.model = Encoding.ASCII.GetBytes(Model);
			fw.header.size = IsBE ?
						IPAddress.HostToNetworkOrder(fw.dataLen) :
						fw.dataLen;

			fw.totalLen = fw.header.size + fw.inFInfo.Length;

			fw.header.totalLen = sizeof(byte) * 4	// sign
					+ sizeof(int) * 2	// start, flash
					+ sizeof(byte) * 4	// model
					+ sizeof(int);		// size

			try
			{
				using (fw.inFs = new FileStream(props.InFile, FileMode.Open,
							FileAccess.Read, FileShare.Read))
				{
					fw.data = new byte[fw.inFInfo.Length];
					Firmware.FileToBytes(in fw.inFs, ref fw.data, fw.inFInfo.Length);
				}
			}
			catch (IOException e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}

			fw.footer.cksum = fw.CalcCksum(IsBE);
			if (props.Debug)
			{
				Console.WriteLine(" header size:\t{0} bytes (0x{0:X})", fw.header.totalLen);
				Console.WriteLine(" data size:\t{0} bytes (0x{0:X})", fw.inFInfo.Length + sizeof(short));
				Console.WriteLine(" total size:\t{0} bytes (0x{0:X})", fw.header.totalLen + fw.inFInfo.Length + sizeof(short));
				Console.WriteLine(" checksum:\t{0:X}\n", fw.footer.cksum);
			}

			/* ヘッダシリアル化 */
			fw.headerData = new byte[fw.header.totalLen];
			if (fw.header.SerializeProps(ref fw.headerData, 0) != fw.header.totalLen)
				return 1;

			/* BEモード時フッタcksumをBE変換 */
			if (IsBE)
				fw.footer.cksum = (ushort)IPAddress.HostToNetworkOrder((short)fw.footer.cksum);
			/* フッタシリアル化 */
			fw.footerData = new byte[sizeof(ushort)];
			fw.footer.SerializeProps(ref fw.footerData, 0);

			return fw.OpenAndWriteToFile(false);
		}
	}
}
