# Handy Backend

C# Handy Backend is a .NET ASP Core Backend for the Handy application.
It picks up API requests and stores deliveryRecords in the database.

The important endpoint we're using is "delivery", it's implemented in the ProductsController

## Useful commands

`scp -r xerographixoffice@192.168.10.247:Projects/Dotnet/HandyBackend/publish .`
Executed on the Windows machine (elevated Powershell) to pull things over to
the Windows machine.

`sc.exe create HandyBackend binPath= "C:¥tmp¥publish¥win-x64¥HandyBackend.exe"`
Creates a Windows service.

`dotnet publish -r win-x64 -c Release -o ./publish/win-x64`
Publish it for windows to the publish directory.

## Windows Security & MySQL Settings (jp)

### MYSQL Workbench手順

- Users and Privilegesでhandybackend用のUserを作成

#### Login

- Limit to Hosts Matching : 127.0.0.1
- password : `appsettings.json`内のPasswordと同一のものを設定

#### Administrative Roles

- DBManagerを許可
- Applyをクリック

### Zipファイルの一括ブロック解除手順

- `handybackend.zip`を任意フォルダに保存
- WindowsPowerShellを起動し`handybackend.zip`が保存されたフォルダへ移動
- `get-childitem -recurse | unblock-File` コマンドを実行
