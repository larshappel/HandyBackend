# 詳細設計書

## 0. 定義
本書は「これを見たら誰でも同じものが作れる設計書」を満たすことを目的とし、画面/入力/処理/データ/エラー/操作の仕様を実装と一致する粒度で記述する。

## 1. 対象/範囲
- 対象: TORIHandyFrontend (Androidアプリ)
- 範囲: 納品登録/設定/未送信データ/送信履歴、入力判定、送信、オフライン対応、自動再送、ログ

## 2. システム構成
- UI: Jetpack Compose
- ロジック: `DeliveryViewModel`
- 永続化: Room (未送信データ)
- 設定保存: DataStore
- 通信: Retrofit + OkHttp
- ログ: 端末ファイル出力

## 3. 画面設計
### 3.1 共通
- 画面構成: トップバー + ナビゲーションドロワー + コンテンツ
- トップバー右側: `v{VERSION_NAME}` + 明/暗切替スイッチ
- スナックバー: 画面上部表示
- デバッグバー: Debugビルドのみ画面下に1行表示

### 3.2 納品登録 (メイン)
表示要素
- 商品ID入力 (必須)
- 数量入力 (必須)
- 個体識別ID入力 (任意)
- 未送信件数表示: `未送信のデータ: {count} 件`
- 送信ボタン: 有効条件 = 商品IDと数量が非空

挙動
- 送信ボタン押下:
  - 入力を検証し、`DeliveryRecord` を生成
  - 送信処理を開始
  - 入力欄をクリアし、商品IDにフォーカス

キー操作
- F1: 商品IDへフォーカス
- F2: 数量へフォーカス
- F3: 個体識別IDへフォーカス
- Enter/F4: 商品IDと数量が入っている場合は送信（フォーカス不問）

### 3.3 設定
- 入力: サーバーIP、ポート、端末ID
- 保存: ボタン押下でDataStoreへ保存し「保存しました」を表示
- 設定画面表示中のみ、5秒ごとに疎通確認し `サーバ設定(接続済/未接続/確認中)` を表示（括弧内のみ色変更）

### 3.4 未送信データ
- バッファ状態: `IDLE`/`未送信あり`/`チェック中`/`再送信中`/`空`
- 未送信一覧表示
- 全件削除ボタン

### 3.5 送信履歴
- 送信履歴一覧（最大200件）
- 成功: 緑系背景、失敗: 赤系背景
- 履歴は端末内DBで永続化（再起動後も保持）

## 4. 入力仕様と判定ロジック
### 4.1 商品ID
入力条件
- 空白不可（空は未入力扱い）
- 数値のみ
- 12桁固定（先頭は`9`）
- 13桁で先頭が `9` の場合は先頭1文字を削除して12桁化
- 上記以外（11桁以下/14桁以上/先頭が9でない）はエラー

エラー文言
- 空: `商品IDは空にできません。`
- 数値以外: `商品IDは数値のみです。`
- 先頭9以外: `このバーコードは商品用ではありません。`
- 桁数不正: `商品IDは12桁です。`

### 4.2 数量
入力条件
- 小数点を1つ含む数値、または数量バーコード
- 小数点がない手入力はエラー（送信時は`数量バーコードではありません。`）
- 先頭ドット入力は `0.x` として許容
- 小数点を除いた桁数は12桁以内

数量バーコード判定
- 先頭5桁が `00000` の数値コード
- `00000` を除いた残りを g とみなす
- kg表記へ変換（例: `000001234` -> `1.234`）

エラー文言
- 空: `数量は空にできません。`
- 数値以外: `数量は数値のみです。`
- 形式不正: `数量の形式が不正です。`
- 小数点なし手入力: `数量バーコードではありません。`
- 先頭5桁が0でない: `数量バーコードではありません。`
- 桁数超過: `数量は12桁以内です。`

### 4.3 個体識別ID
入力条件
- 10桁数値、または `251` で始まり10桁が続くもの
- `251` の直後10桁を抽出（後続が長くても可）
- 判定時は数値以外を無視
- 上記以外は空扱い

### 4.4 自動判定 (スキャン順不同)
判定ルール
- `00000` で始まる数値: 数量
- 10桁数値: 個体識別ID
- 先頭`251`で10桁が続く: 個体識別ID（`251`直後10桁を抽出、後続が長くても可）
- 12桁かつ先頭`9`: 商品ID
- 13桁かつ先頭`9`: 商品ID（先頭1文字を削除）
- その他: エラー

エラー文言
- 数値以外: `数値のみを入力してください。`
- 判定不可: `バーコードを判別できませんでした。`

## 5. 入力受付/ルーティング仕様
### 5.1 連続スキャン耐性
- スキャン入力はバッファリングし、250ms入力停止で確定する
- 0.5秒間隔の連続スキャンを想定

### 5.2 ルーティング処理
1. 入力値をトリム
2. `classifyBarcode` で判定
3. 判定先フィールドへ上書き格納
4. 元フィールドに入っていた値は復元して追記を防止

### 5.3 上書きガード
- フォーカス取得後の最初の入力は既存値を上書きする
- スキャナ入力で末尾追記が発生しないように除去

## 6. 通信仕様
### 6.1 Base URL
- `http://<server_ip>:<port>/`（未入力時は5000）

### 6.2 API
- POST `api/Products/delivery`
  - body: `individual_id`, `product_id`, `amount`, `terminal_id`, `sent_at`
  - response: `{ success: Boolean, message: String }`
- GET `api/health`

### 6.3 送信データ生成
- `terminal_id`: 設定画面の端末ID
- `sent_at`: ISO-8601形式の送信日時

## 7. オフライン対応/再送
- 送信失敗時はRoomへ保存し未送信件数を表示
- 自動再送は5秒間隔で実行
- `/api/health` 成功時に未送信データを再送

## 8. 永続化設計
### 8.1 Room
- DB名: `delivery_database`
- テーブル: `delivery_records`, `send_history`
  - `id` (PK, auto)
  - `individual_id`
  - `product_id`
  - `amount`
  - `timestamp`（`send_history`）
  - `success`（`send_history`）
  - `message`（`send_history`）
- マイグレーション: `MIGRATION_1_2`, `MIGRATION_2_3`

### 8.2 設定 (DataStore)
- `server_ip` (初期値: `192.168.10.247`)
- `server_port` (初期値: `5000`)
- `terminal_id` (初期値: 空)

## 9. ログ設計
- 保存先: `/sdcard/Android/data/<package>/files/logs/`
- 日次ログ: `handy_YYYY-MM-DD.log`
- Debugビルド時は画面下に最新ログを表示（メッセージがある場合のみ）
- 本番ビルドはエラーログのみ保存し、起動時に30日より古いログを削除

## 10. 例外/エラーハンドリング
- 入力不正: スナックバー表示 + ビープ音
- 通信失敗: バッファ保存 + UI通知
- 4xx応答の扱い
  - 400/403/404: 恒久エラー扱いでバッファせず破棄

## 11. 非機能要件
- 連続スキャンで入力欠落が起きない
- Wi-Fi不調でもデータ消失しない
- ログで調査できる

## 12. シーケンス（簡易）
### 12.1 送信フロー
1. ユーザーが送信ボタンを押下
2. 入力値を検証（商品ID/数量）
3. `DeliveryRecord` を生成
4. `DeliveryPayload` を生成（terminal_id/sent_at付与）
5. `POST api/Products/delivery`
6. 成功: 履歴に追加
7. 失敗: Roomへ保存し未送信件数を更新

### 12.2 再送フロー
1. 5秒間隔で未未送信データを確認
2. `GET api/health`
3. 成功: バッファ全件を再送
4. 失敗: バッファ維持

### 12.3 スキャン/入力フロー
1. 入力をバッファリング
2. 250ms入力停止で確定
   - 100msなど短すぎると、低速スキャン/分割入力で途中確定しやすく、誤判定や再入力が増える
3. `classifyBarcode` による判定
4. 判定先フィールドへ上書き格納
5. 元フィールドは復元し追記を防止

## 13. API具体例
### 13.1 送信リクエスト
```json
{
  "individual_id": "1234567890",
  "product_id": "912345678901",
  "amount": "1.234",
  "terminal_id": "BHT-M60-01",
  "sent_at": "2024-05-01T12:34:56Z"
}
```

### 13.2 成功レスポンス
```json
{
  "success": true,
  "message": "OK"
}
```

### 13.3 失敗レスポンス
```json
{
  "success": false,
  "message": "Validation failed"
}
```

## 14. バリデーション/テストケース
### 14.1 商品ID
- 正常: `912345678901` -> `912345678901`
- 正常: `9123456789012` -> `123456789012` (先頭1文字削除)
- 異常: 空 -> `商品IDは空にできません。`
- 異常: `8...` -> `バーコードを判別できませんでした。`
- 異常: `9A...` -> `商品IDは数値のみです。`
- 異常: `91234567890` -> `商品IDは12桁です。`
- 異常: `91234567890123` -> `商品IDは12桁です。`

### 14.2 数量（手入力）
- 正常: `1.000` -> `1.000`
- 正常: `.123` -> `0.123`
- 異常: `1000` -> `数量バーコードではありません。`
- 異常: `1..0` -> `数量の形式が不正です。`
- 異常: `1.a` -> `数量は数値のみです。`

### 14.3 数量（バーコード）
- 正常: `000001234` -> `1.234`
- 異常: `12345` -> `数量バーコードではありません。`

### 14.4 個体識別ID
- 正常: `1234567890` -> `1234567890`
- 正常: `2511234567890` -> `1234567890`
- 正常: `251123456789012345` -> `1234567890`
- 異常: `251123` -> 空扱い

### 14.5 判定不可
- 異常: `ABC123` -> `数値のみを入力してください。`
- 異常: `1234` -> `バーコードを判別できませんでした。`

## 15. 画面遷移
- 起動時: 納品登録(メイン)を表示
- ナビゲーションドロワー:
  - メイン画面 -> 未送信データ -> 送信履歴 -> 設定 へ遷移
  - 設定は連続3回タップで開く
  - 遷移後もトップバー/ドロワー構成は維持

## 16. UIコンポーネント仕様
### 16.1 商品ID入力
- ラベル: `商品ID`
- 入力種別: 数値入力を想定
- フォーカス取得時: 次入力で上書き
- フォーカス喪失時: `routeBarcodeInput` で再判定

### 16.2 数量入力
- ラベル: `数量`
- 入力種別: 数値入力+小数点
- フォーカス喪失時: 文字列が小数点を含む場合のみ `normalizeAmount` 実行

### 16.3 個体識別ID入力
- ラベル: `個体識別ID`
- 入力種別: 数値入力を想定
- フォーカス喪失時: `routeBarcodeInput` で再判定

### 16.4 送信ボタン
- ラベル: 通常 `納品データ送信`
- 有効条件: 商品IDと数量が非空

### 16.5 ProductIdTextField
- props:
  - `value`: 表示値
  - `onValueChange`: 入力更新イベント
  - `focusRequester`: フォーカス制御
  - `onFocusGained`: フォーカス取得時イベント
  - `onFocusLost`: フォーカス喪失時イベント（値を返却）
- events:
  - `onFocusLost` は `routeBarcodeInput` を呼び、必要に応じて補正値を返す

### 16.6 AmountTextField
- props:
  - `value`
  - `onValueChange`
  - `focusRequester`
  - `onFocusGained`
  - `onFocusLost`
- events:
  - `onFocusLost` は `routeBarcodeInput` を呼び、同一フィールドの場合に `normalizeAmount` を実行

### 16.7 IndividualIdTextField
- props:
  - `value`
  - `onValueChange`
  - `focusRequester`
  - `onFocusGained`
  - `onFocusLost`
- events:
  - `onFocusLost` は `routeBarcodeInput` を呼び、必要に応じて補正値を返す

### 16.8 HandySnackbarHost
- props:
  - `snackbarHostState`: 表示状態
  - `modifier`: 表示位置調整
- events:
  - `DeliveryViewModel.uiMessages` を受けて表示

## 16.9 レイアウト/サイズ仕様
### 16.9.1 全体
- メイン画面パディング: `20dp`
- 縦方向間隔: `10dp`
- スナックバー表示位置: 画面上部中央、`padding(16dp)`

### 16.9.2 入力欄
- ラベル/プレースホルダーフォント: `24sp`
- 入力文字フォント: `24sp`
- 単一行入力: `singleLine = true`

### 16.9.3 送信ボタン
- 高さ: `56dp`
- 文字サイズ: `18sp`

### 16.9.4 未送信件数表示
- 文字サイズ: `18sp`

### 16.9.5 デバッグバー
- 文字サイズ: `12sp`
- 余白: `padding(horizontal = 12dp, vertical = 6dp)`

### 16.9.6 未送信データ画面
- 画面パディング: `16dp`
- リスト項目カード: `padding(vertical = 4dp)`、内部 `padding(16dp)`

### 16.9.7 送信履歴画面
- 画面パディング: `16dp`
- リスト項目カード: `padding(vertical = 4dp)`、内部 `padding(16dp)`

### 16.9.8 設定画面
- 画面パディング: `24dp`
- 余白: `Spacer(height = 16dp)`

## 17. 状態管理
- `deliveryBuffer`: 未送信の一覧 (Room)
- `bufferedItemCount`: 未送信件数
- `wasBuffered`: 送信失敗時の入力クリア合図
- `bufferStatus`: バッファ状態表示
- `uiMessages`: スナックバー表示内容

## 18. クラス/責務
- `MainApplication`: `FileLogger` 初期化
- `MainActivity`: Retrofit初期化、IP変更時再生成、Compose起動
- `DeliveryViewModel`:
  - 入力検証/判定/整形
  - 送信/再送制御
  - バッファと件数の管理
- `ServerSettingsRepository`: サーバーIP/端末IDの保存
- `SendHistoryRepository`: 送信履歴の保持(最大200件)

## 19. 関数仕様（主要）
### 19.1 `validateAndTransformOrderId(input)`
- 入力: 商品ID文字列
- 出力: 正規化された商品ID
- 例外: `ValidationException` (空/非数値/先頭9以外)

### 19.2 `normalizeAmount(input)`
- 入力: 数量文字列
- 出力: 正規化された数量
- 例外: `ValidationException` (空/非数値/形式不正)

### 19.3 `classifyBarcode(input)`
- 入力: バーコード文字列
- 出力: `BarcodeResult` (field/value/error)
- 判定: 4.4に準拠

### 19.4 `sendDeliveryData(record)`
- 入力: `DeliveryRecord`
- 出力: なし
- 成功: 送信履歴へ追加
- 失敗: Roomへ保存

## 19.5 関数仕様（詳細）
### 19.5.1 `extractId(input)`
- 入力: 文字列
- 出力: 個体識別ID(10桁)または空文字
- 条件: 10桁数値、または`251`で始まり10桁が続く（後続が長くても可）
- 判定: 数値以外を無視して抽出
- 例外: なし

### 19.5.2 `gramsToKg(gramsText)`
- 入力: g表記の数値文字列
- 出力: kg表記の文字列 (例: `1234` -> `1.234`)
- 例外: 数値変換失敗時に例外

### 19.5.3 `trimAmountPrefix(input)`
- 入力: 数量バーコード文字列
- 出力: `00000`を除いた数値部分
- 例外: `ValidationException` (先頭5桁が0以外/数値不正)

### 19.5.4 `routeBarcodeInput(rawValue, sourceField, sourceFocusRequester)`
- 入力: 生入力/入力元/フォーカス要求
- 出力: ルーティング後の値（元フィールド用）
- 副作用: 判定先フィールドを上書き、エラー表示/ビープ（エラー時のみフォーカス復帰）

### 19.5.5 `scheduleAutoRoute(...)`
- 入力: フィールド種別/値取得/値設定/ジョブ管理
- 出力: なし
- 副作用: 遅延後に自動判定を実行

### 19.5.6 `applyOverwriteGuard(currentValue, incomingValue, shouldClear)`
- 入力: 既存値/新値/上書きフラグ
- 出力: 次の値/次のフラグ
- 目的: スキャン時の末尾追記を除去

### 19.5.7 `checkServerStatus()`
- 入力: なし
- 出力: `Response<Unit>`
- 呼び出し元: 自動再送ループ

### 19.5.8 `sendDelivery(delivery)`
- 入力: `DeliveryPayload`
- 出力: `Response<ApiResponse>`
- 呼び出し元: 送信/再送処理

### 19.5.9 `getServerIpFlow(context)` / `setServerIp(context, ip)`
- 入力: Context/IP
- 出力: Flow/String
- 副作用: DataStore書き込み

### 19.5.10 `getTerminalIdFlow(context)` / `setTerminalId(context, id)`
- 入力: Context/端末ID
- 出力: Flow/String
- 副作用: DataStore書き込み

### 19.5.11 `deliveryRecordDao.insert(record)`
- 入力: `DeliveryRecord`
- 出力: なし
- 副作用: Roomに1件追加

### 19.5.12 `deliveryRecordDao.deleteAll()` / `deleteById(id)`
- 入力: ID（deleteByIdのみ）
- 出力: なし
- 副作用: Roomから削除

### 19.5.13 `SendHistoryRepository.addHistoryEntry(entry)`
- 入力: `SendHistoryEntry`
- 出力: なし
- 副作用: 先頭に追加し最大200件に制限

### 19.5.14 `FileLogger.init(context)`
- 入力: Context
- 出力: なし
- 副作用: ログ保存ディレクトリ準備

### 19.5.15 `FileLogger.log(tag, message)` / `error(tag, message)`
- 入力: タグ/メッセージ
- 出力: なし
- 副作用: logcatとファイル出力、Debug時は画面表示更新

## 20. バッファ状態遷移
- `BUFFER_IS_EMPTY`: 未送信0件
- `BUFFER_CONTAINS_VALUES`: 未送信あり
- `CHECKING_FOR_ENDPOINT`: ヘルスチェック中
- `ATTEMPTING_RESEND`: 再送信中
- `IDLE`: 初期状態

## 21. 入力イベント詳細
- スキャンバッファ確定: 250ms入力停止
- 自動ルーティング猶予: 250ms
- 連続スキャン判定: 1500ms以内の入力はスキャン扱い
- キーコード:
  - F1: 131
  - F2: 132
  - F3: 133
  - F4: 134（送信）
  - スキャン: 501

## 22. 通信/エラー方針
- 送信失敗時の再送判定: 例外/非200応答を失敗扱い
- 4xx応答の扱い: 400/403/404は破棄

## 23. パーミッション/ネットワーク設定
- `android.permission.INTERNET`
- `android.permission.ACCESS_NETWORK_STATE`
- `network_security_config`: cleartext許可

## 24. ビルド設定
- Debug: `applicationIdSuffix = ".dev"`
- バージョン表示: `BuildConfig.VERSION_NAME`
- Compose有効化、KSP/Hilt/Room利用

## 25. テスト/検証チェックリスト
- 入力判定: 商品ID/数量/個体識別の各パターン
- 連続スキャン: 0.5秒間隔で入力欠落がない
- 通信失敗: 未送信件数が増える
- 再送: Wi-Fi復旧後に自動再送される
- 設定: IP/端末IDが保持される
- 履歴: 最大200件で古いものが削除される
