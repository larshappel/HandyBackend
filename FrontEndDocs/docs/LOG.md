docs/LOG.md
======

目的
- 現在アプリが出力しているログの一覧を整理する。

出力先
- Logcat: `Log.d` / `Log.e`
- 端末内ファイル: `FileLogger` により `/sdcard/Android/data/<package>/files/logs/handy_YYYY-MM-DD.log`
- Debug画面下部ステータス: `DebugStatus`（Debugビルドのみ）

ビルド種別ごとの挙動（現状）
- Debugビルド
  - Logcat: 全ログが出る
  - 端末内ファイル: `FileLogger` のログが保存される
  - 画面下ステータス: `DebugStatus` が表示される
- 本番ビルド
  - Logcat: `FileLogger`/`ScanDebug` 由来のログは出さない
  - 端末内ファイル: エラーログのみ保存（`FileLogger.error`）
  - 画面下ステータス: 表示されない（Debugビルドのみ）

ログ保持ポリシー
- 端末内ログは日次ファイルで保存
- 起動時に30日より古いログを削除し、直近30日分を保持
- 削除ロジック（概要）
  - 対象: `handy_YYYY-MM-DD.log` 形式のファイルのみ
  - ファイル名の日付を解析し、解析できない場合は最終更新日時で判定
  - 30日より古いものを削除

ログ一覧

1) FileLogger (共通)
- 位置: `app/src/main/java/com/example/handytestapp/utils/FileLogger.kt`
- 形式: `YYYY-MM-DD HH:mm:ss.SSS [TAG] message`
- Logcatへも `Log.d(tag, message)` / `Log.e(tag, message)` で出力
- Debugビルド時は `DebugStatus` にも反映

2) 商品ID入力欄 (ScanDebug)
- 位置: `app/src/main/java/com/example/handytestapp/ui/components/ProductIdTextField.kt`
- 出力:
  - `ProductId focus gained`
  - `ProductId focus lost. Value: <value>`
  - `ProductId focus event: hide keyboard`

3) 数量入力欄 (ScanDebug)
- 位置: `app/src/main/java/com/example/handytestapp/ui/components/AmountTextField.kt`
- 出力:
  - `Amount focus gained`
  - `Amount focus lost. Value: <value>`
  - `Amount focus event: hide keyboard`

4) 個体識別ID入力欄 (ScanDebug)
- 位置: `app/src/main/java/com/example/handytestapp/ui/components/IndividualIdTextField.kt`
- 出力:
  - `IndividualId focus gained`
  - `IndividualId focus lost. Value: <value>`
  - `IndividualId focus event: hide keyboard`

5) 明/暗トグル (ScanDebug, Logcatのみ)
- 位置: `app/src/main/java/com/example/handytestapp/ui/screens/MainScreen.kt`
- 出力:
  - `Theme toggle focus: true/false`

6) 受信した生バーコード (ScanDebug, Logcatのみ)
- 位置: `app/src/main/java/com/example/handytestapp/ui/screens/DeliveryEntryScreen.kt`
- 出力:
  - `Raw barcode input: <rawValue> (source=<sourceField>)`

7) デバッグ/操作系 (Debug)
- 位置: `app/src/main/java/com/example/handytestapp/ui/screens/DeliveryEntryScreen.kt`
- 出力:
  - `Manual input in amount field.`
  - `Scanned the amount field.`
  - `Focus shifted to individualIdFocusRequester`
  - `Focus shifted to amountFocusRequester`
  - `Button was focussed? Buttonfocus: <bool> and the focusState: <bool>`
  - `Sending delivery data: OrderID=<orderID>, IndividualID=<individualID>, Amount=<amount>`

高速スキャン時の削除プラン
- 目的: 高速スキャンの原因追跡に必要なログだけ残す。
- 方針: 「スキャン入力の生値」と「フォーカス遷移」だけ残し、他は削除する。

残すログ (高速スキャン調査で必要)
- `Raw barcode input: <rawValue> (source=<sourceField>)`
- `ProductId/Amount/IndividualId focus gained/lost`
- `ProductId/Amount/IndividualId focus event: hide keyboard`

削除対象 (スピードテスト時は削除)
- `Manual input in amount field.`
- `Scanned the amount field.`
- `Focus shifted to individualIdFocusRequester`
- `Focus shifted to amountFocusRequester`
- `Button was focussed? Buttonfocus: ...`
- `Theme toggle focus: true/false`
