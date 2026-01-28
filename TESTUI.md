# TEST UI（/check）の仕様と使い方

## 目的
- Handy Backend が受信した配信記録を、フロントエンド・端末からの挙動・DB 更新の痕跡として素早く確認するための簡易手段です。
- `/check` へアクセスすると、送信先情報、受信ログ、Product（orderdetails）スナップショットを一覧できます。

## 画面構成とAPI
1. **送信先（受け側）の IP とポート**
   - `/api/check/info`（`CheckController.GetDestinationInfo`）から取得した `scheme`/`host`/`port` を表示します。
   - 「ローカル IPv4: 192.168.10.18 PORT:5001」のように複数 IPv4 アドレスを `/` 区切りで表示し、PORT も併記するように UI を改修済みです。
2. **最新の受信ログ（client-access）は UI から削除**
   - UI ではログパネルを廃止し、代わりに `logs/client-access-*.txt` を直接確認してください。必要なら `curl http://localhost:5001/api/check/logs` で JSON を取得して内容と一致するか確認できます。
3. **Product スナップショット（orderdetails テーブル）**
   - `/api/check/products`（`CheckController.GetProductSnapshots`）で `orderdetails` の最新 50 行を取得し、OrderDetailId/Amount/LabelCollectCount/Individual ID/UpdatedAt をテーブル表示します。
   - 例外が起きた場合は `Problem(detail: "Product snapshot取得エラー: ...")` を返すので、UI はその本文をテーブルの1行目に表示し、画面で何が起きたか分かるようになっています。

## バックエンドの起動手順（テスト用 DB を使う）
1. **SHIRAKIPR DB を作成**
   - `mysql -u root -e "CREATE DATABASE IF NOT EXISTS SHIRAKIPR CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"` を実行。
   - `Migrations/20250626041632_InitialCreate.cs` で `orderdetails` テーブルを定義済みなので、`dotnet ef database update` を流すと正しいスキーマ（`OrderDetailRef`, `SalesQuantity`, `LabelCollectCount`, `UpdateDate`, `UpdateTime` など）が作成されます。
2. **本番接続を変えずに接続文字列を上書いてマイグレーション／起動**
   ```bash
   ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;Database=SHIRAKIPR;User=root;Password=" \
     /Users/xingyang/.dotnet/tools/dotnet-ef database update
   ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;Database=SHIRAKIPR;User=root;Password=" dotnet run
   ```
   - `dotnet-ef` は `~/.dotnet/tools/dotnet-ef` にインストール済みなので、`export PATH="$PATH:/Users/xingyang/.dotnet/tools"` を `.zprofile` に入れるとなお便利です。
3. **UI にアクセス**
   - `http://localhost:5001/check` をブラウザで開き、ページを読み込むと自動で `/api/check/info`/`/logs`/`/products` を叩きます。
   - 「最新情報を取得」ボタンで手動更新できます。

## テストの流れ
1. `dotnet run` をバックグラウンドで立ち上げた状態で別端末（例：実機）から `POST /api/products/delivery` を送信。
2. `/check` 画面の「最新の受信ログ」に記録が追加されているか確認。
3. `Product snapshot` に新しい `OrderDetailId` が反映されるか、`UpdatedAt` が UTC で更新されているか、`LabelCollectCount` が増えているかなどをチェック。

## 失敗時の切り分け
- `Product snapshot取得エラー: ...` が出た場合は `logs/backend-log-YYYYMMDD.txt` を確認。`CheckController` 側で `ILogger.LogError` も出力しているためスタックトレースを追えます。
- `/api/check/products` には 500 を返すとき `Problem(detail: エラーメッセージ, statusCode: 500)` で返すので、画面に出るメッセージそのものが原因説明になります。
- 典型的な失敗：`orderdetails` が実際の DB に存在しない／`OrderDetailRef` の UNIQUE 制約に違反している／接続文字列が別 DB（`handybackend`）を向いている。

## その他補足
- 送信ログのソース（`logSource`）が `null` のときは `logs/client-access-*.txt` が見つからなかったと判断して「ログファイル未検出」と表示します。
- `check/index.html` は簡易な静的ファイルなので、必要であれば `npm run build` 相当のフロントはありません。UI 変更があるときはこの HTML/JS を直接編集して `Destination` 表示や `Table` の列を追加してください。
- 実機から送信する際は `logs/client-access-*.txt` に「timestamp, productId, amount, individualId, message」形式で `CsvHeaderHooks` がヘッダを付けるので、それに沿ってデバッグできます。

## 本番への移行と注意点
- **DB マイグレーションは直接本番 DB には流さないでください。**  
  `TESTUI` は本番と同じ `orderdetails` スキーマを模してローカルで動かすもので、`dotnet ef database update` は原則としてテスト用 `SHIRAKIPR` や複製した DB でのみ実行します。本番サーバに MIGRATION を適用する際は、まずバックアップを取得し、差分に含まれるカラム名/型/制約が現行スキーマと一致するか慎重に確認してください。
- **本番環境からデータをコピーする場合の手順**  
  1. 本番 `handybackend` DB をダンプ（`mysqldump` など）してローカルに `SHIRAKIPR` をリストアすると、UI と API をほぼ同じ条件で検証できます。  
  2. `OrderDetailId` の値が 9001 〜 9999 に入っていないときは、対象の行を手動挿入して UI 側から送って動作確認してください（`insert` で `OrderDetailRef` に `357` など数値を入れておけば `product_id="900100000357"` としてマッチします）。  
  3. 本番と共有する `logs/client-access` を使う場合は、ファイルパス・アクセス権・ログローテーションが本番と一致しているかを確認してから運用してください（`CustomLogger` 設定と `CsvHeaderHooks` も合わせて確認）。
- **本番から移行するときのリスク管理**  
  - 本番アップデート前に最新の `logs/` や `orderdetails` のバックアップを取る。  
  - コード変更（`ProductsController` の仕様や `ApplicationDbContext` のマッピングなど）を伴う場合は必ず staging で検証し、UI の `Product snapshot取得エラー` が出ても `logs/backend-log-*.txt` で詳細を追って原因を絞る。  
  - `TESTUI` で使用している `ConnectionStrings__DefaultConnection` など環境変数の上書き方法をそのまま本番にコピーせず、運用環境では `appsettings.json` のまま動かすようにしてください（本番ログは `CustomLogger:ClientAccessLogPath` に沿って記録されます）。
