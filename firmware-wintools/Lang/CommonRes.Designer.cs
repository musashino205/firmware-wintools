﻿//------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン:4.0.30319.42000
//
//     このファイルへの変更は、以下の状況下で不正な動作の原因になったり、
//     コードが再生成されるときに損失したりします。
// </auto-generated>
//------------------------------------------------------------------------------

namespace firmware_wintools.Lang {
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
    internal class CommonRes {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CommonRes() {
        }
        
        /// <summary>
        ///   このクラスで使用されているキャッシュされた ResourceManager インスタンスを返します。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("firmware_wintools.Lang.CommonRes", typeof(CommonRes).Assembly);
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
        ///   Common Options:
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_CommonOpts {
            get {
                return ResourceManager.GetString("Help.CommonOpts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Function Options:
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_FunctionOpts {
            get {
                return ResourceManager.GetString("Help.FunctionOpts", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -i &lt;file&gt;		input file
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_i {
            get {
                return ResourceManager.GetString("Help.Options.i", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -o &lt;file&gt;		output file
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_o {
            get {
                return ResourceManager.GetString("Help.Options.o", resourceCulture);
            }
        }
        
        /// <summary>
        ///     -Q			suppress output of information messages
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Help_Options_Q {
            get {
                return ResourceManager.GetString("Help.Options.Q", resourceCulture);
            }
        }
    }
}