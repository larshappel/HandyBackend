# 納品データ送信API（受け取り側向け）

目的
- ハンディ端末から納品データを送信するAPIの入力形式を明確化する。

懸念点（例付き）
1) HTTP 200でも `success=false` を成功扱いにしてしまう
- 例: 受け側が `{"success": false, "message": "Validation failed"}` を返しているのに、
  アプリは成功として履歴に追加・再送バッファを空にしてしまう。
  → 実際は未受理なのに成功扱いになり、データ欠損の原因になる。
- 対象ファイル: `app/src/main/java/com/example/handytestapp/DeliveryViewModel.kt`
- コード抜粋:
```kotlin
when (response.code()) {
    200, 204 -> {
        postMessage("納品データが正常に送信されました", UiMessageStyle.Success)
        SendHistoryRepository.addHistoryEntry(
            SendHistoryEntry(deliveryRecord = delivery, success = true, message = "送信成功")
        )
        sendBufferedData()
    }
    // ...
}
```

2) 400/403/404 は恒久エラー扱いで破棄する
- 例: 403/404 を一時的な障害として返すと、ハンディ側は破棄する。
  一時障害は 5xx で返す運用が必要。
- 対象ファイル: `app/src/main/java/com/example/handytestapp/DeliveryViewModel.kt`
- コード抜粋:
```kotlin
400, 403 -> {
    sendBufferedData()
    val errorMessage = response.body()?.message ?: "データのフォーマットを確認してください"
    postMessage(errorMessage, UiMessageStyle.Error)
    SendHistoryRepository.addHistoryEntry(
        SendHistoryEntry(deliveryRecord = delivery, success = false, message = errorMessage)
    )
}
404 -> {
    sendBufferedData()
    val errorMessage = response.body()?.message ?: "商品IDが見つかりませんでした"
    postMessage(errorMessage, UiMessageStyle.Error)
    SendHistoryRepository.addHistoryEntry(
        SendHistoryEntry(deliveryRecord = delivery, success = false, message = errorMessage)
    )
}
```

3) タイムアウト/接続断時の重複送信
- 例: サーバーは受信してDBに書き込み済みだが、通信断でクライアントは失敗扱い。
  → バッファ再送で同じデータが二重登録される可能性がある。
- 対象ファイル: `app/src/main/java/com/example/handytestapp/DeliveryViewModel.kt`
- コード抜粋:
```kotlin
} catch (e: Exception) {
    withContext(Dispatchers.Main) {
        postMessage("ネットワークエラー、データをバッファしました。", UiMessageStyle.Error)
        SendHistoryRepository.addHistoryEntry(
            SendHistoryEntry(deliveryRecord = delivery, success = false, message = "ネットワークエラー")
        )
        addDeliveryToBuffer(delivery)
    }
}
```
```kotlin
if (response.isSuccessful) {
    deliveryRecordDao.deleteById(record.id)
    withContext(Dispatchers.Main) {
        SendHistoryRepository.addHistoryEntry(
            SendHistoryEntry(deliveryRecord = record, success = true, message = "送信成功 (バッファから)")
        )
    }
} else {
    // 再送失敗時に停止
    break
}
```

4) `terminal_id` が空でも送信される
- 例: 受け側で `terminal_id` を必須にしている場合、空文字で受理失敗になる。
  → 送信失敗なのにアプリ側では成功扱いになる可能性がある。
- 対象ファイル: `app/src/main/java/com/example/handytestapp/DeliveryViewModel.kt`
- コード抜粋:
```kotlin
val terminalId = withTimeoutOrNull(100) {
    runCatching {
        ServerSettingsRepository.getTerminalIdFlow(application).first().trim()
    }.getOrDefault("")
} ?: ""
```

送信先
- Base URL: `http://<server_ip>:5000/`
- Endpoint: `POST api/Products/delivery`
- Content-Type: `application/json`

送信タイミング
- 送信ボタン押下で送信
- 送信失敗時は端末内にバッファ保存し、アプリ起動中に再送を試行
- `/api/health` が成功した場合に再送を開始

リクエストボディ
```json
{
  "individual_id": "1639895531",
  "product_id": "001000001579",
  "amount": "0.146",
  "terminal_id": "A1-01",
  "sent_at": "2026-01-21T08:47:10.516Z"
}
```

各項目の意味
- `individual_id`: 個体識別番号（任意。未入力の場合は空文字）
- `product_id`: 商品ID（必須。12桁固定、先頭9）
- `amount`: 数量（必須。小数点を含む文字列）
- `terminal_id`: 端末ID（設定画面で入力。未設定の場合は空文字）
- `sent_at`: 送信時刻（ISO-8601 UTC、例: `2026-01-21T08:47:10.516Z`）

レスポンス
- 成功: HTTP 200 / 204
```json
{
  "success": true,
  "message": "OK"
}
```

- 失敗: HTTP 400/403/404/5xx
```json
{
  "success": false,
  "message": "Validation failed"
}
```

エラー時の扱い（アプリ側）
- 400/403/404: 恒久エラーとして破棄（バッファ保存はしない）
- 5xx/通信失敗: バッファ保存して後で再送

備考
- Debugビルドは `applicationIdSuffix = ".dev"` の別アプリとして動作
- サーバー側に重複排除がない場合、再送で二重登録になる可能性がある
