﻿//------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン:4.0.30319.42000
//
//     このファイルへの変更は、以下の状況下で不正な動作の原因になったり、
//     コードが再生成されるときに損失したりします。
// </auto-generated>
//------------------------------------------------------------------------------

namespace firmware_wintools.Lang.Tools {
    using System;
    
    
    /// <summary>
    ///   ローカライズされた文字列などを検索するための、厳密に型指定されたリソース クラスです。
    /// </summary>
    // このクラスは StronglyTypedResourceBuilder クラスが ResGen
    // または Visual Studio のようなツールを使用して自動生成されました。
    // メンバーを追加または削除するには、.ResX ファイルを編集して、/str オプションと共に
    // ResGen を実行し直すか、または VS プロジェクトをビルドし直します。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class MkSenaoFwRes {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal MkSenaoFwRes() {
        }
        
        /// <summary>
        ///   このクラスで使用されているキャッシュされた ResourceManager インスタンスを返します。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("firmware_wintools.Lang.Tools.mksenaofw.MkSenaoFwRes", typeof(MkSenaoFwRes).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   すべてについて、現在のスレッドの CurrentUICulture プロパティをオーバーライドします
        ///   現在のスレッドの CurrentUICulture プロパティをオーバーライドします。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   failed to calculate header checksum に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_FailHeaderCksum {
            get {
                return ResourceManager.GetString("Error.FailHeaderCksum", resourceCulture);
            }
        }
        
        /// <summary>
        ///   failed to load data に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_FailLoadData {
            get {
                return ResourceManager.GetString("Error.FailLoadData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   failed to load header に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_FailLoadHeader {
            get {
                return ResourceManager.GetString("Error.FailLoadHeader", resourceCulture);
            }
        }
        
        /// <summary>
        ///   the length of version is invalid に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_InvalidVerLen {
            get {
                return ResourceManager.GetString("Error.InvalidVerLen", resourceCulture);
            }
        }
        
        /// <summary>
        ///   input file is too large (&gt;= 2 GiB) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_LargeInFile {
            get {
                return ResourceManager.GetString("Error.LargeInFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   firmware type &quot;combo&quot; is not supported in this program に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_NoImplCombo {
            get {
                return ResourceManager.GetString("Error.NoImplCombo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   no or invalid firmware type is specified に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_NoInvalidFwType {
            get {
                return ResourceManager.GetString("Error.NoInvalidFwType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   vendor or product is not specified or invalid に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_NoInvalidVenProd {
            get {
                return ResourceManager.GetString("Error.NoInvalidVenProd", resourceCulture);
            }
        }
        
        /// <summary>
        ///   encode/decode firmware for the devices manufactured by Senao に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FuncDesc {
            get {
                return ResourceManager.GetString("FuncDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -b &lt;blocksize&gt;	use the &lt;blocksize&gt; for padding image
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_b {
            get {
                return ResourceManager.GetString("Help.Options.b", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -d			use &quot;decode&quot; mode instead of &quot;encode&quot;
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_d {
            get {
                return ResourceManager.GetString("Help.Options.d", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -m &lt;magic&gt;		use &lt;magic&gt; for image header
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_m {
            get {
                return ResourceManager.GetString("Help.Options.m", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -p &lt;product&gt;		use &lt;product&gt; for image header
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_p {
            get {
                return ResourceManager.GetString("Help.Options.p", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -r &lt;vendor&gt;		use &lt;vendor&gt; for image header
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_r {
            get {
                return ResourceManager.GetString("Help.Options.r", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -t &lt;type&gt;		use image &lt;type&gt; for image header
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_t {
            get {
                return ResourceManager.GetString("Help.Options.t", resourceCulture);
            }
        }
        
        /// <summary>
        ///   			----------------------------------
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_t_Line {
            get {
                return ResourceManager.GetString("Help.Options.t.Line", resourceCulture);
            }
        }
        
        /// <summary>
        ///   			 {0,2} = {1,-12} {2} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_t_TypeFmt {
            get {
                return ResourceManager.GetString("Help.Options.t.TypeFmt", resourceCulture);
            }
        }
        
        /// <summary>
        ///   			--- valid image &lt;type&gt; values: --- に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_t_values {
            get {
                return ResourceManager.GetString("Help.Options.t.values", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -v &lt;version&gt;		use &lt;version&gt; for image header
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_v {
            get {
                return ResourceManager.GetString("Help.Options.v", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -z			enable image padding to &lt;blocksize&gt;
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_z {
            get {
                return ResourceManager.GetString("Help.Options.z", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Usage: {0}mksenaofw [OPTIONS...]
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Usage {
            get {
                return ResourceManager.GetString("Help.Usage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   &quot;===== mksenaofw mode ({0}) =====&quot; に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info {
            get {
                return ResourceManager.GetString("Info", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Block Size	: 0x{0:X} ({0} bytes) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_BS {
            get {
                return ResourceManager.GetString("Info.BS", resourceCulture);
            }
        }
        
        /// <summary>
        ///   decode に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_Decode {
            get {
                return ResourceManager.GetString("Info.Decode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   encode に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_Encode {
            get {
                return ResourceManager.GetString("Info.Encode", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Firmware Type	: {0} ({1}) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_FwType {
            get {
                return ResourceManager.GetString("Info.FwType", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Firmware Ver.	: {0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_FwVer {
            get {
                return ResourceManager.GetString("Info.FwVer", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Header Magic	: {0:X} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_Magic {
            get {
                return ResourceManager.GetString("Info.Magic", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Data MD5 sum	: {0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_MD5 {
            get {
                return ResourceManager.GetString("Info.MD5", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Padding Image	: {0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_Pad {
            get {
                return ResourceManager.GetString("Info.Pad", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Product ID	: 0x{0:X} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_Product {
            get {
                return ResourceManager.GetString("Info.Product", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Vendor ID	: 0x{0:X} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_Vendor {
            get {
                return ResourceManager.GetString("Info.Vendor", resourceCulture);
            }
        }
        
        /// <summary>
        ///       {0}		: {1} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Main_FuncDesc_Fmt {
            get {
                return ResourceManager.GetString("Main.FuncDesc.Fmt", resourceCulture);
            }
        }
    }
}
