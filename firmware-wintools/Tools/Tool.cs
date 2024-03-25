using System;
using System.Collections.Generic;
using System.Reflection;
using static firmware_wintools.Tools.Param;

namespace firmware_wintools.Tools
{
	public class Param
	{
		public char PChar;
		public PTYPE PType;
		public string SetField = "";
		public string SetBool = "";
		public string HelpKey;
		public object HelpOpt;
		public bool Override;

		public enum PTYPE
		{
			INT = 0,
			LONG,
			BOOL,
			STR,
		};
	}

	public abstract class Tool
	{
		public string _name = "";
		public virtual string name { get => ""; }
		public virtual string desc { get => ""; }
		public virtual string descFmt { get => ""; }
		public virtual string resName { get => ""; }
		public virtual bool skipOFChk { get => false; }

		internal virtual List<Param> ParamList { get; }

		internal abstract int
		Do(string[] args, int arg_idx, Program.Properties baseProps);

		internal int InitArgs(string[] args, int offset)
		{
			string tmp = "", errtype;

			for (int i = offset; i < args.Length; i++) {
				if (!args[i].StartsWith("-") || args[i].Length < 2)
					continue;

				Param p = ParamList.Find(x =>
						x.PChar.ToString() == args[i].Substring(1));
				if (p == null)
					continue;

				FieldInfo setf = GetType().GetField(p.SetField,
						BindingFlags.Instance |
						BindingFlags.NonPublic |
						BindingFlags.DeclaredOnly);
				FieldInfo setb = GetType().GetField(p.SetBool,
						BindingFlags.Instance |
						BindingFlags.NonPublic |
						BindingFlags.DeclaredOnly);

				/*
				 * 対象のフィールドが見付からない場合
				 * - setfがnull
				 * - SetBool指定時にsetbがnull
				 */
				if (setf == null ||
				    (p.SetBool.Length > 0 && setb == null))
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							Lang.CommonRes.Error_FailedGetField,
							setf == null ?
								p.SetField : p.SetBool);
					return -1;
				}

				if (p.PType != PTYPE.BOOL &&
					!Utils.GetStrParam(args, i, out tmp))
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							Lang.CommonRes.Error_FailedGetOptArg,
							p.PChar);
					return -22;
				}

				errtype = "";
				switch (p.PType)
				{
					case PTYPE.INT:
						if (Utils.StrToInt(tmp, out int cnvInt,
								System.Globalization.NumberStyles.None))
							setf.SetValue(this, cnvInt);
						else
							errtype = "int";
						break;
					case PTYPE.LONG:
						if (Utils.StrToLong(tmp, out long cnvLong,
								System.Globalization.NumberStyles.None))
							setf.SetValue(this, cnvLong);
						else
							errtype = "long";
						break;
					case PTYPE.BOOL:
						setf.SetValue(this, true);
						break;
					case PTYPE.STR:
						setf.SetValue(this, tmp);
						break;
					default:
						return -22;
				}

				if (!errtype.Equals(""))
				{
					Console.Error.WriteLine(Lang.Resource.Main_Error_Prefix +
							Lang.CommonRes.Error_FailParseVal,
							tmp, errtype);
					return -22;
				}

				if (setb == null)
					continue;

				setb.SetValue(this, true);
			}

			return 0;
		}

		private string GetResText(Type t, string key)
		{
			PropertyInfo pi;

			if (t == null || key == null)
				return null;

			pi = t.GetProperty(key,
					BindingFlags.Static | BindingFlags.NonPublic);
			return pi == null ? null : (string)pi.GetValue(null);
		}

		internal void PrintHelp(int argIdx)
		{
			Type t = Type.GetType("firmware_wintools.Lang.Tools." + resName);
			string buf, tmp;

			if (t == null)
				return;

			/* 先頭部 */
			if ((tmp = GetResText(t, "Help_Usage")) == null)
				return;
			buf = tmp;
			if ((tmp = GetResText(t, "FuncDesc")) == null)
				return;
			buf += tmp;
			buf += Environment.NewLine;
			Console.WriteLine(buf, argIdx < 2 ? "" : "firmware-wintools ");

			/* 共通オプション */
			Program.PrintCommonOption();

			/* 機能オプション */
			buf = Lang.CommonRes.Help_FunctionOpts;
			foreach (Param p in ParamList) {
				if ((tmp = GetResText(t, p.HelpKey)) == null)
					continue;
				buf += (p.HelpOpt == null) ?
					tmp : string.Format(tmp, p.HelpOpt);
			}
			Console.WriteLine(buf);
		}
	}
}
