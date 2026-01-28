# テスト用 MySQL 構築手順（ローカル）

このバックエンドをローカルで確認するには `appsettings.json` で定義されている `handybackend` DB に加えて、テスト用の専用データベース／ユーザーを用意するのが安全です。

## 推奨設定（ローカル専用・本番には影響なし）
- テストデータベース: `handybackend_dev`
- 既存本番データベース: `handybackend`
- ユーザー: `handytester`
- パスワード: `TestPass123!`

## 1. MySQL サーバーを起動
macOS + Homebrew では `mysql.server start` で起動しますが、既存の PID ファイルが残っていると失敗することがあります。
1. `/opt/homebrew/var/mysql` に `Yukihiros-Macbook-AIr-M3.local.pid` 等が残っている場合は `rm` か `mv` で移動します（SIP により一部ファイルは削除できないことがあるので、別パスへコピーしてから `mysql.server start`。`
2. それでも起動しない場合は `mysqld --pid-file=/tmp/mysql.pid --socket=/tmp/mysql.sock --port=3306` のように PID/ソケットを別指定して起動してみてください。

## 2. データベースとユーザーの作成
手動で SQL を叩くのが面倒な場合は、`scripts/setup-test-db.sh` を実行すると一括で作成できます。

```bash
scripts/setup-test-db.sh
```

（`MYSQL_USER` / `MYSQL_PASS` 環境変数を設定すれば別の認証情報でも実行できます。）

### 手動で SQL を叩く場合
MySQL に接続できるようになったら、以下を実行してテスト用 DB/ユーザーを作成します。

```sql
CREATE DATABASE IF NOT EXISTS handybackend_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'handytester'@'localhost' IDENTIFIED BY 'TestPass123!';
GRANT ALL PRIVILEGES ON handybackend_dev.* TO 'handytester'@'localhost';
FLUSH PRIVILEGES;
```

## 3. マイグレーション実行
`handybackend_dev` を使うには `appsettings.Development.json` か `dotnet user-secrets` で `DefaultConnection` を差し替えた上で、

```bash
dotnet ef database update
```

を叩くと、`Migrations/` に収められたスキーマが反映されます。

## 4. 接続確認と UI への反映
`dotnet run` を実行して `http://localhost:5001/check` を開き、「送信先情報」「受信ログ」「Product スナップショット」がエラーなしに取得できれば準備完了です。

> **備考:** このテスト構成はローカル専用なので、パスワード `TestPass123!` やユーザー `handytester` は本番には使わないようにしてください。

## 5. 本番に影響を与えず接続先を切り替える
既存の `appsettings.json` を変更することなくテスト用 DB と接続先を切り替えるには、実行時に接続文字列を上書きする方法を採用してください。

1. **コマンドライン環境変数で上書く**  
   ```bash
   ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;Database=SHIRAKIPR;User=root;Password=" dotnet ef database update
   ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;Database=SHIRAKIPR;User=root;Password=" dotnet run
   ```
   この方法だと `appsettings.json` には触れずに `SHIRAKIPR` を使ったマイグレーション／起動が可能です。`dotnet run` が停止すれば元の設定に戻るため、本番環境には影響がありません。

2. **開発専用設定ファイルを使う**  
   `appsettings.Development.json` に次のような接続情報を追加すると、`ASPNETCORE_ENVIRONMENT=Development` で起動したときにのみテスト DB に切り替わります。
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=127.0.0.1;Port=3306;Database=SHIRAKIPR;User=root;Password="
     }
   }
   ```
   CI や本番では `appsettings.json` を使うため、こちらも環境ごとの切り替えになります。

3. **永続的な変更が不要な場合はコマンド実行後に元に戻す**  
   一時的に別接続を使ったあとは環境変数を解除、あるいは `appsettings.Development.json` を再調整すれば元の `handybackend` 設定に戻ります。
