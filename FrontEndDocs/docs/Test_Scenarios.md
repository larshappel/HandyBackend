# Test Scenarios / テストシナリオ

## Purpose / 目的
- Validate barcode scanning (order-agnostic routing), manual entry, error handling, buffering, and resend behavior. / バーコードスキャン（順不同振り分け）、手入力、エラー処理、バッファリング、自動再送の動作を検証する。

## Environment / 環境
- Device: Denso BHT-M60 (Android 7+) / 端末: Denso BHT-M60 (Android 7+)
- App: Debug build with separate applicationIdSuffix / アプリ: debugビルド（別applicationIdSuffix）
- Network: Wi-Fi available + Wi-Fi disabled modes / ネットワーク: Wi-Fi有り/無し

## Core Happy Paths / 基本シナリオ
1) Scan Product ID -> Amount -> Individual ID -> Send
1) 商品ID→数量→個体識別→送信
2) Scan Amount -> Product ID -> Individual ID -> Send (order-agnostic)
2) 数量→商品ID→個体識別→送信（順不同）
3) Scan Individual ID -> Product ID -> Amount -> Send (order-agnostic)
3) 個体識別→商品ID→数量→送信（順不同）
4) Product ID + Amount only, Individual ID omitted -> Send
4) 商品ID+数量のみ、個体識別省略で送信
5) Manual Amount entry with decimal -> Send
5) 数量の手入力（小数点あり）で送信
6) Scan Product ID + Scan Amount + Manual Individual ID -> Send
6) 商品ID・数量スキャン＋個体識別手入力で送信
7) Scan Product ID + Manual Amount + Scan Individual ID -> Send
7) 商品IDスキャン＋数量手入力＋個体識別スキャンで送信
8) Re-scan Product ID replaces previous value (no append)
8) 商品ID再スキャンで上書き（追記なし）
9) Re-scan Amount replaces previous value (no append)
9) 数量再スキャンで上書き（追記なし）
10) Re-scan Individual ID replaces previous value (no append)
10) 個体識別再スキャンで上書き（追記なし）

## Barcode Classification Rules / 判定ルール
11) Product ID: numeric 12 digits, starts with 9
11) 商品ID: 12桁の数値（先頭9）
12) Product ID: 13 digits starting with 9 -> remove first digit to 12
12) 商品ID: 13桁で先頭9→先頭1文字を削除して12桁化
13) Amount: starts with 00000 -> g to kg conversion (e.g., 000001234 -> 1.234)
13) 数量: 00000開始はg→kg変換（例: 000001234→1.234）
14) Individual ID: exactly 10 digits
14) 個体識別: 10桁数値
15) Individual ID: 251 + 10 digits (prefix, longer OK) -> extract 10 digits
15) 個体識別: 251+10桁（先頭一致、後続が長くても可）は10桁を採用

## Input Focus and Function Keys / フォーカスとキー
16) F1 focuses Product ID
16) F1で商品IDにフォーカス
17) F2 focuses Amount
17) F2で数量にフォーカス
18) F3 focuses Individual ID
18) F3で個体識別にフォーカス
19) Enter sends when Product ID + Amount filled (focus-agnostic)
19) Enterは商品ID+数量が入っていれば送信（フォーカス不問）
20) F4 sends when Product ID + Amount filled (focus-agnostic)
20) F4は商品ID+数量が入っていれば送信（フォーカス不問）
21) Focus changes do not clear existing values unless overwritten by new scan
21) フォーカス移動では既存値は消えない（新スキャン時のみ上書き）

## Error Handling (Irregular / Negative) / エラー系
21) Product ID empty -> send disabled
21) 商品ID空欄→送信不可
22) Product ID starts not with 9 -> error, no value accepted
22) 商品IDが先頭9以外→エラー
23) Product ID contains non-digit -> error
23) 商品IDが数値以外→エラー
24) Product ID length is not 12 or 13 -> error
24) 商品IDが12/13桁以外→エラー
25) Amount manual entry without decimal -> error
25) 数量手入力で小数点なし→エラー
26) Amount manual entry with two decimals (e.g., 1.2.3) -> error
26) 数量手入力で小数点複数→エラー
27) Amount manual entry with leading dot (e.g., .5) -> accepted as 0.5
27) 数量手入力で先頭ドット→0.5として採用
28) Amount manual entry with trailing dot (e.g., 1.) -> error
28) 数量手入力で末尾ドット→エラー
29) Amount length (digits only) exceeds 12 -> error
29) 数量の桁数（小数点除外）が12桁超→エラー
30) Amount barcode without 00000 prefix -> error ("Not amount barcode")
30) 数量バーコードが00000で始まらない→エラー
31) Amount barcode with non-digit -> error
31) 数量バーコードに非数値→エラー
32) Individual ID non-digit -> error
32) ignored / 個体識別に非数値→エラー/無視
33) Individual ID 9 digits -> error
33) ignored / 個体識別9桁→エラー/無視
34) Individual ID 11 digits -> error
34) ignored / 個体識別11桁→エラー/無視
35) Individual ID 251 + 9 digits -> error
35) ignored / 個体識別251+9桁→エラー/無視
36) Barcode does not match any rule -> error ("Unclassified")
36) どのルールにも一致しない→エラー

## Order-Agnostic Scan Edge Cases / 順不同の境界ケース
36) Scan Amount while Product ID field is focused -> amount routed correctly
36) 商品ID欄フォーカス中に数量スキャン→数量に格納
37) Scan Individual ID while Amount field is focused -> ID routed correctly
37) 数量欄フォーカス中に個体識別スキャン→個体識別に格納
38) Scan Product ID while Individual ID field is focused -> product routed correctly
38) 個体識別欄フォーカス中に商品IDスキャン→商品IDに格納
39) Scan same field twice quickly -> only last value retained
39) 同一欄を連続スキャン→最後の値のみ残る
40) Scan different fields back-to-back within 0.5s -> values routed correctly
40) 0.5秒間隔で別種スキャン→正しく振り分け

## High-Speed Scanning / 高速スキャン
41) 0.5s interval: Product -> Amount -> Individual ID, no data loss
41) 0.5秒間隔: 商品→数量→個体識別、欠落なし
42) 0.5s interval: Amount -> Product -> Individual ID, no data loss
42) 0.5秒間隔: 数量→商品→個体識別、欠落なし
43) 0.5s interval: Individual ID -> Product -> Amount, no data loss
43) 0.5秒間隔: 個体識別→商品→数量、欠落なし
44) Continuous 30 scans (mixed order) -> no freeze
44) no input loss / 連続30スキャン（順不同）でフリーズなし/欠落なし

## Buffering and Resend / バッファと再送
45) Wi-Fi off -> send -> record stored in buffer
45) Wi-Fi断→送信→バッファ保存
46) Buffer count increments on failure
46) 送信失敗で件数が増える
47) App restart -> buffer preserved
47) アプリ再起動後もバッファ保持
48) Wi-Fi on -> auto resend every 5 seconds
48) Wi-Fi復旧後5秒間隔で自動再送
49) Partial resend failure -> stop resend and keep remaining items
49) 部分失敗時は再送停止・残り保持
50) Manual buffer clear -> all buffered items removed
50) バッファ手動クリアで全削除

## Send History / 送信履歴
51) Successful send -> history entry created
51) 成功送信で履歴追加
52) Failed send -> history entry created with error
52) 失敗送信でエラー履歴追加
53) History list capped at 200 (older entries dropped)
53) 履歴は200件上限で古いものから削除

## Settings / 設定
54) Change server IP -> new value persists
54) サーバーIP変更が永続化
55) Change terminal ID -> new value persists
55) 端末ID変更が永続化
56) Restart app -> saved settings loaded
56) 再起動後も設定が復元

## Payload Validation / 送信ペイロード
57) Send payload contains product_id, individual_id, amount
57) product_id・individual_id・amountが含まれる
58) Send payload contains terminal_id
58) terminal_idが含まれる
59) Send payload contains sent_at (ISO-8601)
59) sent_atが含まれる（ISO-8601）

## UI and UX / UI・UX
60) Version label visible in top bar
60) トップバーにバージョン表示
61) Snackbar appears on error with correct message
61) エラー時にスナックバー表示
62) Debug status bar visible in debug build
62) Debugビルドでステータス表示
63) Focus returns to Product ID after send
63) 送信後に商品IDへフォーカス復帰

## Irregular / Abuse Tests (Stress) / ストレス・異常系
64) Random rapid key input (monkey) -> app remains responsive
64) ランダム連打でも応答維持
65) Long random barcode (50+ chars) -> error, no crash
65) 50文字超バーコードでエラー・クラッシュなし
66) Mixed digits and symbols -> error, no crash
66) 数字と記号混在→エラー・クラッシュなし
67) Alternating scans with manual typing -> no crashes
67) スキャンと手入力交互でもクラッシュなし
68) Rapid switching between screens during scan -> no crashes
68) スキャン中の画面切替でもクラッシュなし

## Recovery Tests / リカバリ
69) App killed during buffering -> buffer intact after relaunch
69) バッファ中に強制終了→再起動後も保持
70) Server returns 400 -> not buffered
70) 400応答時はバッファしない
71) Server returns 403/404 -> not buffered
71) 403/404応答時はバッファしない
72) Server returns 5xx -> buffer saved
72) 5xx応答時はバッファ保存
