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
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class AesRes {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal AesRes() {
        }
        
        /// <summary>
        ///   このクラスで使用されているキャッシュされた ResourceManager インスタンスを返します。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("firmware_wintools.Lang.Tools.aes.AesRes", typeof(AesRes).Assembly);
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
        ///   the length of target data is not a multiple of 16 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_InvalidDecLen {
            get {
                return ResourceManager.GetString("Error.InvalidDecLen", resourceCulture);
            }
        }
        
        /// <summary>
        ///   the length of specified IV is invalid for hex mode に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_InvalidIVLenHex {
            get {
                return ResourceManager.GetString("Error.InvalidIVLenHex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   the length of specified key is invalid for hex mode に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_InvalidKeyLenHex {
            get {
                return ResourceManager.GetString("Error.InvalidKeyLenHex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   the length of specified IV exceeds 16 bytes に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_LongIVLen {
            get {
                return ResourceManager.GetString("Error.LongIVLen", resourceCulture);
            }
        }
        
        /// <summary>
        ///   the length of specified IV exceeds 16 bytes (32 characters) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_LongIVLenHex {
            get {
                return ResourceManager.GetString("Error.LongIVLenHex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   the length of specified key exceeds the key length {0} bytes に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_LongKeyLen {
            get {
                return ResourceManager.GetString("Error.LongKeyLen", resourceCulture);
            }
        }
        
        /// <summary>
        ///   the length of specified key exceeds the key length {0} bytes ({1} characters) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_LongKeyLenHex {
            get {
                return ResourceManager.GetString("Error.LongKeyLenHex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   no key specified に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Error_NoKey {
            get {
                return ResourceManager.GetString("Error.NoKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   encrypt/decrypt firmware with 128/256 bit AES CBC に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FuncDesc {
            get {
                return ResourceManager.GetString("FuncDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -d			use decryption mode
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_d {
            get {
                return ResourceManager.GetString("Help.Options.d", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -k &lt;key&gt;		use text &lt;key&gt; for encryption/decryption the image
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_k {
            get {
                return ResourceManager.GetString("Help.Options.k", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -K &lt;key&gt;		use hex &lt;key&gt; for encryption/decryption the image
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_K2 {
            get {
                return ResourceManager.GetString("Help.Options.K2", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -l &lt;length&gt;		encrypt/decrypt specified length of the input image
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_l {
            get {
                return ResourceManager.GetString("Help.Options.l", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -O &lt;offset&gt;		encrypt/decrypt from specified offset of the input image
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_O2 {
            get {
                return ResourceManager.GetString("Help.Options.O2", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -s			use 128-bit key (default: 256-bit)
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_s {
            get {
                return ResourceManager.GetString("Help.Options.s", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -v &lt;iv&gt;		use text &lt;iv&gt; for encryption/decryption the image
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_v {
            get {
                return ResourceManager.GetString("Help.Options.v", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -V &lt;iv&gt;		use hex &lt;iv&gt; for encryption/decryption the image
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_V2 {
            get {
                return ResourceManager.GetString("Help.Options.V2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Usage: {0}aes [OPTIONS...]
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Usage {
            get {
                return ResourceManager.GetString("Help.Usage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   ===== aes mode ({0}) ===== に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info {
            get {
                return ResourceManager.GetString("Info", resourceCulture);
            }
        }
        
        /// <summary>
        ///   decrypt に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_Decrypt {
            get {
                return ResourceManager.GetString("Info.Decrypt", resourceCulture);
            }
        }
        
        /// <summary>
        ///   encrypt に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_Encrypt {
            get {
                return ResourceManager.GetString("Info.Encrypt", resourceCulture);
            }
        }
        
        /// <summary>
        ///    IV		: {0}
        ///		  ({1}) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_iv {
            get {
                return ResourceManager.GetString("Info.iv", resourceCulture);
            }
        }
        
        /// <summary>
        ///    IV		: {0} (hex) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_iv2 {
            get {
                return ResourceManager.GetString("Info.iv2", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Key		: {0}
        ///		  ({1}) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_key {
            get {
                return ResourceManager.GetString("Info.key", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Key		: {0} (hex) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_key2 {
            get {
                return ResourceManager.GetString("Info.key2", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Length		: {0:N0} bytes
        ///		  (0x{0:X}) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_len {
            get {
                return ResourceManager.GetString("Info.len", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Mode		: {0} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_mode {
            get {
                return ResourceManager.GetString("Info.mode", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Offset		: {0:N0} bytes
        ///		  (0x{0:X}) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Info_offset {
            get {
                return ResourceManager.GetString("Info.offset", resourceCulture);
            }
        }
        
        /// <summary>
        ///       {0}			: {1} に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Main_FuncDesc_Fmt {
            get {
                return ResourceManager.GetString("Main.FuncDesc.Fmt", resourceCulture);
            }
        }
        
        /// <summary>
        ///   the specified length is invalid, use default (fullsize - offset) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Warning_InvalidLength {
            get {
                return ResourceManager.GetString("Warning.InvalidLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   the specified offset exceeds the file length, use &apos;0&apos; に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Warning_LargeOffset {
            get {
                return ResourceManager.GetString("Warning.LargeOffset", resourceCulture);
            }
        }
        
        /// <summary>
        ///   no iv specified, use default (&apos;0&apos;) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Warning_NoIV {
            get {
                return ResourceManager.GetString("Warning.NoIV", resourceCulture);
            }
        }
        
        /// <summary>
        ///   the length of the last block on the target data is shorter than 16 bytes, will padded by zero に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Warning_ShortEncLen {
            get {
                return ResourceManager.GetString("Warning.ShortEncLen", resourceCulture);
            }
        }
    }
}
