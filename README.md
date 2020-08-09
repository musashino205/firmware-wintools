# firmware-wintools

OpenWrtのfirmware-utilsのWindows移植（という名の適当再現）

@musashino205 が学習目的で作成しています。

## ヘルプ

```firmware-wintools -h```

```firmware-wintools <func> -h``` （機能ヘルプ）

## 機能

- aes
- buffalo-enc
- mkedimaximg
- mksenaofw
- nec-enc
- xorimage

---

### aes

OpenWrtのfirmware-utilsには存在しない独自機能です。AES-128-CBCまたはAES-256-CBCによる暗号化/復号を提供します。

使用方法:

encryption: ```firmware-wintools aes -i <input file> -o <output file> -k <text key> [-v <text iv>] [-l <length>] [-O <offset>] [-s]```

(or ```firmware-wintools aes -i <input file> -o <output file> -K <hex key> [-V <hex iv>] [-l <length>] [-O <offset>] [-s]```)

decryption: ```firmware-wintools aes -d -i <input file> -o <output file> -k <text key> [-v <text iv>] [-l <length>] [-O <offset>] [-s]```

(or ```firmware-wintools aes -d -i <input file> -o <output file> -K <hex key> [-V <hex iv>] [-l <length>] [-O <offset>] [-s]```)

注意:

- デフォルトでは256bitの鍵長を使用します。128bitを使用する場合、 ```-s``` を指定してください。

---

### buffalo-enc

OpenWrtにおけるbuffalo-encの機能を提供します。

使用方法:

encryption: ```firmware-wintools buffalo-enc -i <input file> -o <output file> -p <product> -v <version> [-m <magic>] [-k <key>] [-S <size>] [-l]```

decryption: ```firmware-wintools buffalo-enc -i <input file> -o <output file> -d [-k <key>] [-O <offset>] [-l]```

---

### mkedimaximg

OpenWrtにおけるmkedimaximgの機能を提供します。

使用方法:

```firmware-wintools mkedimaximg -i <input file> -o <output file> -s <signature> -m <model> -f <flash> -S <start> [-b]```

---

### mksenaofw

OpenWrtにおけるmksenaofwの機能を提供します。

使用方法:

encode: ```mksenaofw -i <input file> -o <output file> -t <type> -r <vendor> -p <product> [-v <version>] [-z] [-b <blocksize>]```

decode: ```mksenaofw -i <input file> -o <output file> -d```

注意:

- 本プログラムにおける引数解析の仕組み上、encode/decodeの指定や入力ファイル指定に用いるオプションをオリジナルのものから変更しています。

- ファームウェアタイプ 0 については、日本国内の機種で用いられている事例は今のところ確認していないため、実装は省略しています。

---

### nec-enc

OpenWrtにおけるnec-encの機能を提供します。

使用方法:

```firmware-wintools nec-enc -i <input file> -o <output file> -k <key>```

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

```firmware-wintools xorimage -i <input file> -o <output file> -p <pattern> [-x]```

注意:

```-x``` オプションは、patternに文字列で表現することができない16進数を使用する必要がある場合に用います。

---

## 対応言語

- 日本語 (Japanese)
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

- Windows 10 64bit (1903)
- .NET Framework 4.8

## バージョン履歴

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
