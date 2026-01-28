# Handy Backend

C# Handy Backend is a .NET ASP Core Backend for the Handy application.
C# Handy Backend は Handy アプリ向けの .NET ASP Core バックエンドです。
It picks up API requests and stores deliveryRecords in the database.
API リクエストを受け取り、deliveryRecords をデータベースへ保存します。

The important endpoint we're using is "delivery", it's implemented in the ProductsController
主要エンドポイントは `delivery` で、`ProductsController` 内で実装されています。

## Useful commands

Building, deploying, copying...
ビルド、デプロイ、コピー用のコマンド例です。

`scp -r xerographixoffice@192.168.10.247:Projects/Dotnet/HandyBackend/publish .`
Executed on the Windows machine (elevated Powershell) to pull things over to
the Windows machine.
`scp -r xerographixoffice@192.168.10.247:Projects/Dotnet/HandyBackend/publish .`
（Windows マシン上の昇格済み PowerShell で実行し、publish ディレクトリを取得します。）

`sc.exe create HandyBackend binPath= "C:¥tmp¥publish¥win-x64¥HandyBackend.exe"`
Creates a Windows service.
`sc.exe create HandyBackend binPath= "C:¥tmp¥publish¥win-x64¥HandyBackend.exe"`
（Windows サービスを作成します。）

`dotnet publish -r win-x64 -c Release -o ./publish/win-x64`
Publish it for windows to the publish directory.
`dotnet publish -r win-x64 -c Release -o ./publish/win-x64`
（Windows 向けに `./publish/win-x64` へ出力します。）

## Logging

The position of the client facing logfile is specified in `appsettings.json` ->
CustomLogger -> ClientAccessLogPath.
クライアント向けログファイルのパスは `appsettings.json` の `CustomLogger -> ClientAccessLogPath` で指定します。

## ローカル確認用テストDB
ローカル検証時は新しいデータベース `handybackend_dev` と専用ユーザー `handytester`（パスワード `TestPass123!`）を作成し、`docs/TEST_DATABASE.md` に記載した手順で初期化してください。
これは検証用の設定なので、上記ユーザー／パスワードは本番には使わず、`appsettings.json` に記載の `handybackend` など既存の接続情報のまま運用してください。

## Windows Security & MySQL Settings (jp)

### MYSQL Workbench手順

- Users and Privilegesでhandybackend用のUserを作成
- `Users and Privileges` で handybackend 用のユーザーを作成します。

#### Login

- Limit to Hosts Matching : 127.0.0.1
- password : `appsettings.json`内のPasswordと同一のものを設定
- `Limit to Hosts Matching : 127.0.0.1`
- `password` は `appsettings.json` 内の `Password` と同じ値にします。

#### Administrative Roles

- DBManagerを許可
- Applyをクリック
- `DBManager` ロールを付与し、`Apply` をクリックします。

### Zipファイルの一括ブロック解除手順

- `handybackend.zip`を任意フォルダに保存
- WindowsPowerShellを起動し`handybackend.zip`が保存されたフォルダへ移動
- `get-childitem -recurse | unblock-File` コマンドを実行
- `handybackend.zip` を任意のフォルダへ保存します。
- Windows PowerShell を開き、`handybackend.zip` のあるフォルダへ移動します。
- `get-childitem -recurse | unblock-File` コマンドを実行し、ZIP 内のファイルを一括でブロック解除します。
