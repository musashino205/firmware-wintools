# firmware-wintools

OpenWrtのfirmware-utilsのWindows移植（という名の適当再現）

@musashino205 が学習目的で作成しています。

## ヘルプ

```firmware-wintools -h```

```firmware-wintools <func> -h``` （機能ヘルプ）

## 機能

- aes
- bincut
- buffalo-enc
- mkedimaximg
- mksenaofw
- nec-enc
- xorimage

---

### aes

OpenWrtのfirmware-utilsには存在しない独自機能です。AES-128-CBCまたはAES-256-CBCによる暗号化/復号を提供します。

使用方法:

- encryption:
  ```
  firmware-wintools aes -i <input file> -o <output file> -k <text key> [-v <text iv>] [-l <length>] [-O <offset>] [-s]

  (or)
  firmware-wintools aes -i <input file> -o <output file> -K <hex key> [-V <hex iv>] [-l <length>] [-O <offset>] [-s]
  ```

- decryption:
  ```
  firmware-wintools aes -d -i <input file> -o <output file> -k <text key> [-v <text iv>] [-l <length>] [-O <offset>] [-s]

  (or)
  firmware-wintools aes -d -i <input file> -o <output file> -K <hex key> [-V <hex iv>] [-l <length>] [-O <offset>] [-s]
  ```

注意:

- デフォルトでは256bitの鍵長を使用します。128bitを使用する場合、 ```-s``` を指定してください。

---

### bincut

OpenWrtのfirmware-utilsには存在しない独自機能です。指定された長さ/オフセットによるファームウェアの切り出しや、パディングサイズまたはブロックサイズによるパディングを提供します。

使用方法:

```
firmware-wintools bincut -i <input file> -o <output file> [-l <length>] [-O <offset>] [-p <padding size>]

(or)
firmware-wintools bincut -i <input file> -o <output file> [-l <length>] [-O <offset>] [-P <blocksize>]
```

注意:

- パディングは、長さやオフセットが同時に指定されている場合は、それにより切り出された後の長さを基準にして行われます。

---

### buffalo-enc

OpenWrtにおけるbuffalo-encの機能を提供します。

使用方法:

- encryption:
  ```
  firmware-wintools buffalo-enc -i <input file> -o <output file> -p <product> -v <version> [-m <magic>] [-k <key>] [-S <size>] [-l]
  ```

- decryption:
  ```
  firmware-wintools buffalo-enc -i <input file> -o <output file> -d [-k <key>] [-O <offset>] [-l]
  ```

---

### mkedimaximg

OpenWrtにおけるmkedimaximgの機能を提供します。

使用方法:

```
firmware-wintools mkedimaximg -i <input file> -o <output file> -s <signature> -m <model> -f <flash> -S <start> [-b]
```

---

### mksenaofw

OpenWrtにおけるmksenaofwの機能を提供します。

使用方法:

- encode:
  ```
  mksenaofw -i <input file> -o <output file> -t <type> -r <vendor> -p <product> [-v <version>] [-z] [-b <blocksize>]
  ```

- decode:
  ```
  mksenaofw -i <input file> -o <output file> -d
  ```

注意:

- 本プログラムにおける引数解析の仕組み上、encode/decodeの指定や入力ファイル指定に用いるオプションをオリジナルのものから変更しています。

- ファームウェアタイプ 0 については、日本国内の機種で用いられている事例は今のところ確認していないため、実装は省略しています。

---

### nec-enc

OpenWrtにおけるnec-encの機能を提供します。

使用方法:

```
firmware-wintools nec-enc -i <input file> -o <output file> -k <key>
```

確認済み機種:

- Aterm WG1200CR

恐らく対応する機種:

- Aterm WR8165N
- Aterm WR8166N
- Aterm WF300HP2
- Aterm WF800HP
- Aterm WF1200CR
- Aterm WG2600HS
- その他Realtek SoCを搭載する11acモデル全般

注意:

WG2600HPxシリーズやMRxxLNシリーズ、WR4100N及びその類似機種はファームウェアの暗号化方法が異なるため、使用できません。

---

### xorimage

OpenWrtにおけるxorimageの機能を提供します。

使用方法:

```
firmware-wintools xorimage -i <input file> -o <output file> -p <pattern> [-x] [-O <offset>] [-l <length>] [-r]
```

注意:

- ```-x``` オプションは、patternに文字列で表現することができない16進数を使用する必要がある場合に用います。
- ```-r``` オプションは、入力ファイル内の一部データにのみXorを行う場合に用います。

---

## 特殊機能

- firmware-wintoolsバイナリのファイル名を機能名にリネームするか、もしくは機能名でsymlinkを作成すると直接機能を呼び出すことができます。

  symlinkを作成する場合、拡張子 ```.exe``` を付けて作成する必要があります。実行時は ```.exe``` を付けなくても実行可能です。

  例:

  ```
  > mklink buffalo-enc.exe firmware-wintools.exe
  > ./buffalo-enc
  ```

  実行結果:

  ```
  使用方法: buffalo-enc [オプション...]
  バッファロー製デバイスのファームウェアを暗号化/復号します

  共通オプション:
    -i <ファイル>		入力元ファイル
    -o <ファイル>		出力先ファイル
    -Q			情報関連メッセージの出力を抑制します

  機能オプション:
    -d			復号モードを使用します
    -l			ロングステート 暗号化/復号メソッドを使用します
    -k <キー>		暗号化に用いる <キー> を指定します（デフォルト: "Buffalo"）
    -m <マジック>		暗号化に用いる <マジック> を指定します（デフォルト: "start"）
    -p <プロダクト名>	暗号化に用いる <プロダクト名> を指定します
    -v <バージョン>	暗号化に用いる <バージョン> を指定します
    -O <オフセット>	ファイル内における暗号化されたデータのオフセットを指定します（復号用）
    -S <サイズ>		ファイル内における未暗号化データのサイズを指定します (暗号化用)
    -F			チェックサム エラーを無視して復号します
  ```

## 対応言語

- 日本語 (Japanese)
  - 簡易機能を除く

- 英語 (English)

## 開発方針

- 自分の学習と理解が最優先
- 最新のWindows環境で全ての機能が利用できること
- 可能な限りWindowsの機能のみで動作し、外部ツールを必要としないこと

### 保留中の実装

- 標準入出力によるデータの入出力
  - PowerShellのリダイレクトやパイプはbyteデータをそのまま出力せず、データが壊れるため。
  - cmd.exe, git-bashでは正常に機能する。

## 動作要件

- Windows OS
  - 型付け周りが非常に雑な為、OSのbit数等、環境によっては正しく動作しない可能性もあります。

- .NET Framework 4.7.2

### 動作確認済環境

- Windows 10 64bit
  - 1903
  - 2004
  - 21H1

- Windows 11 64bit
  - 21H2

- .NET Framework 4.8

## バージョン履歴

- 0.6.5.1 (2022/11/30): nec-bsdffsモードにおいて、ディレクトリinodeデータパース時に例外を吐くことがあった問題を修正
- 0.6.5 (2022/11/28): 複数の簡易的な機能を追加, ヘルパー関数をいくつか追加, 機能呼び出しを改善, 既定以外の実行ファイル名を許可, aesモードの簡略化をrevert, その他
- 0.6.1.2 (2022/11/07): aesモードにおける暗号化/復号部分を簡略化
- 0.6.1.1 (2022/11/07): aesモードにおけるkey/ivのエラーチェックを修正/改善, 全体におけるファイル情報表示をデバッグ時のみに制限
- 0.6.1 (2022/11/07): Tool, Utilsの2クラスを追加, 各機能をToolの派生クラスに変換
- 0.6.0 (2021/08/09): Firmwareクラスを追加, 各機能で使用、buffalo-encでWZR-600DHP2/900DHPのチェックサム算出をサポート、機能クラスを静的クラス化
- 0.5.8 (2021/08/09): nec-encキー長制限を32byteに拡大、サイズ/オフセット指定において補助単位(k/m/g)をサポート
- 0.5.7.3 (2020/12/26): .gitattributesの修正、.editorconfig追加、bincutヘルプの使用方法の誤りを修正
- 0.5.7 (2020/12/26): Gitタグのテストとリリースビルド、リリース作成用のGitHub Actions Workflowを追加
- 0.5.6.2 (2020/12/19): bincutモード追加、不要なコードのクリーンアップと整理
- 0.5.5 (2020/12/19): symlinkによる機能呼び出しサポート追加
- 0.5.4.4 (2020/12/14): xorimageのヘルプにおいて、追加し忘れた2つのオプションの説明を追加
- 0.5.4.3 (2020/12/11): xorimageモードにlength, offset引数サポート追加と部分書き換えモード追加, メッセージ出力抑制オプション追加, i/o/Q引数の共通オプション化, その他細かなコード整理
- 0.5.3.0 (2020/08/30): nec-encモードに "ハーフ" モードを追加
- 0.5.2.0 (2020/08/30): aesモードに16バイトチェックを追加
- 0.5.1.2 (2020/08/09): aesモードのファイルサイズ < 0x80000000制限を撤廃, メッセージ出力先の整理, aesモード中心のコード整理
- 0.5.0 (2020/08/08): aes 追加
- 0.4.1.1 (2020/04/15): buffalo-encにforceオプションを追加
- 0.4.1 (2020/04/13): 全体的なコードのクリーンアップ実施とbuffalo-encにおけるoffsetとsizeでの16進数値サポート
- 0.4.0.1 (2019/07/20): mksenaofwに 5 - 12 のイメージ タイプを追加
- 0.4.0 (2019/07/20): i18nサポートを追加
- 0.3.1 (2019/07/17): 本体ヘルプの表示を修正
- 0.3.0 (2019/07/17): mksenaofw 追加
- 0.2.0 (2019/07/15): buffalo-enc, mkedimaximg, xorimage 追加
- 0.1.0 (2019/07/07): 初版（nec-enc）

## ライセンス

MIT

## Thanks

- OpenWrt
  - project team
  - all contributors

- Microsoft Docs

- アドバイス頂いたMastodonユーザー各位
