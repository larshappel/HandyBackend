# 納品エンドポイント試験仕様

## 適用範囲

`ProductsController.ProcessDelivery` の振る舞い契約を対象とし、妥当な納品要求が記録され、無効な要求が仕様どおりのレスポンスで拒否されることを確認する。

## 共通テストセットアップ

- コントローラの単体テストは `ProductsController` を対象とし、`IProductService`
  をモック、`ILogger<ProductsController>` はテスト用ロガーで捕捉する。
- 特に指定がない限り、モックされたサービスには次のプロパティを持つ `Product`
  をシードする: `Id = 42`, `OrderDetailId = 123456`, `Amount = 10.0`,
  `LabelIssueCount = 5`, `LabelCollectCount = 2`, `IdentificationNumber = null`。
- 正常系では `UpdateProductAsync`
  が数量変化、ラベルカウントのインクリメント、タイムスタンプ更新を反映した
  `Product` を受け取ることを検証する。

## テストケース

### TC1 正常系 – キログラム表記

準備: 注文詳細 ID で照会された際にサービスがシード済みプロダクトを返し、`UpdateProductAsync` には更新後のプロダクトを返す。
データベースには product_id `9000123456` の行が存在し、数量が 10kg の状態で登録されている。
対象商品の `LabelCollectCount` は 0、`LabelIssueCount` は 5。

操作: 次のペイロードで POST する。
`{
product_id: "9000123456",
amount: "1.25",
individual_id: "1234567890",
device_id: "1"
}`

確認: HTTP 200 で `message == "Delivery processed successfully"`,
  `productOrderDetailId == 123456`, `newAmount == 11.25` を返す。
  `LabelCollectCount` が 1 にインクリメントされ、`IdentificationNumber` が
  `1234567890`
  になること、固定テストクロックを導入している場合はタイムスタンプがその値に更新されることを検証する。

### TC2 正常系 – グラムからの換算

準備: TC1 と同じ。

操作: 次のペイロードで POST する。
`{
product_id: "9000123456",
amount: "750",
individual_id: "1234567890",
device_id: "1"
}`

確認: HTTP 200 で `newAmount == 10.75` を返す。
数量差分が `0.75` (グラム→キログラム換算) であり、その他の更新 (LabelCollectCount のインクリメントなど) も TC1 と同様に行われることを確かめる。

### TC3 プレフィックスより短い Product ID

準備: リクエスト前処理で失敗するため、サービスは呼び出されない。

操作: `{
product_id: "123",
amount: "1.0",
individual_id: "1",
device_id: "1"
}` を POST する。

確認: HTTP 400 と `{ message: "Invalid Product ID format." }` のペイロードを返し、サービス呼び出しは行われないこと。

### TC4 プレフィックス除去後が数値でない Product ID

準備: サービスは呼び出されない。

操作: `{
product_id: "9999ABCD",
amount: "1.0",
individual_id: "1",
device_id: "dev"
}` を POST する。

確認: HTTP 400 と `{ message: "Invalid Product ID format." }` のペイロードを返し、更新処理が呼ばれないことを確認する。

### TC5 未登録の Product

準備: `GetProductByOrderDetailIdAsync` が `null` を返し、product_id `9000123456` がデータベースに存在しない状態を表現する。

操作: `{
product_id: "9000123456",
amount: "1.0",
individual_id: "1",
device_id: "1"
}` を POST する。

確認: HTTP 404 と `{ message: "Product '9000123456' not found" }`
を返し、`UpdateProductAsync` が一切呼ばれないことを検証する。

### TC6 数量フォーマットが無効

準備: サービスは呼び出されない。

操作: `{
product_id: "9000123456",
amount: "not-a-number",
individual_id: "1",
device_id: "1"
}` を POST する。

確認: HTTP 400 と `{ message: "Invalid amount format." }` を返し、プロダクトの変異やサービス更新が行われないこと。

### TC7 ラベルスキャン上限到達

準備: 対象プロダクトが DB に存在し、`LabelCollectCount == LabelIssueCount` の状態になっている。

操作: `{
product_id: "9000123456",
amount: "1.0",
individual_id: "1",
device_id: "1"
}` を POST する。

確認: HTTP 200 と `{ message: "It's already scanned!" }` を返し、`UpdateProductAsync` が呼ばれず元のカウントが維持されることを検証する。

### TC8 個人 ID が数値化できない

準備: サービスがシード済みプロダクトを返し、更新が完了する。

操作: `{
product_id: "9000123456",
amount: "1.0",
individual_id: "abc",
device_id: "1"
}` を POST する。

確認: HTTP 200
の成功レスポンスを返し、数量とラベルカウントは更新される一方で、`IdentificationNumber`
は `null` (または以前の値) のままであること。

### TC9 サービス更新結果が null

準備: `GetProductByOrderDetailIdAsync` はプロダクトを返すが、`UpdateProductAsync` が `null` を返し、行が削除された競合を模擬する。

操作: 妥当なペイロードで POST する。

確認: HTTP 200 と `{ message: "The product no longer exists." }` のペイロードを返し、それ以上の更新が試行されないこと。

### TC10 サービスがラベル上限例外をスロー

準備: `GetProductByOrderDetailIdAsync` はシードされたプロダクトを返す一方で、`ApplyDeliveryAsync` がラベル上限を強制するために `InvalidOperationException` をスローするようモックする。

操作: 妥当な納品ペイロードを POST する。

確認: HTTP 200 と `{ message: "It's already scanned!" }` のペイロードを返し、`ApplyDeliveryAsync` が 1 回だけ呼ばれ、再試行や代替の更新処理が行われないことを検証する。

### TC11 個人 ID が long の上限を超過

準備: サービスはシード済みプロダクトを返し、`ApplyDeliveryAsync` は正常に完了する。

操作: 妥当な納品ペイロードを POST するが、`individual_id` に 25 桁の 9 など `long.MaxValue` を超える数値文字列を指定する。

確認: HTTP 200 の成功レスポンスを返し、見た目が数値でもコントローラが `identificationNumber` に `null` を渡すこと、およびサービスが期待どおりの数量差分を受け取ることを検証する。

### TC12 クライアントアクセスログ

準備: サービスは標準プロダクトを返すようにモックし、スコープ情報を記録できるテストロガーでログを捕捉する。

操作: 成功更新につながる妥当な納品ペイロードを POST する。

確認: 同一メッセージを持つログが 2 件だけ存在し、1 件はスコープなし、もう 1 件は `LogType == "ClientAccess"` を含むスコープ内で出力されることを検証する。追加の同一メッセージログがないことも確認する。

### TC13 カンマ区切り数量の拒否

準備: 共通ハーネス設定を用い、テストロガーでログを捕捉する。

操作: `amount` だけを `"1,25"`（カンマ小数）に変更した TC1 相当のリクエストを、カルチャを `CultureInfo.InvariantCulture` に固定した状態で POST する。

確認: HTTP 400 と `{ message: "Invalid amount format." }` を返し、`GetProductByOrderDetailIdAsync` と `ApplyDeliveryAsync` が呼び出されないこと、さらにクライアントアクセスログに "Invalid amount format" が記録されることを検証する。

## 未解決事項 / 追加検討点

- カンマを含む数量表記を将来的に許容する必要があるか。現状は TC13 で拒否を仕様化しているため、ローカライズ要件の変更時に再検討する。
- クライアント向けログを統合テストでも検証すべきか、それとも TC12/TC13
  の単体テストカバレッジで要件を満たすかを確認する。
