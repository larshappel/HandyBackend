# 受信データ確認UI（ローカル用）

目的
- ハンディ端末から送信された納品データを、ローカルPC上で人が目視確認するための簡易UI。
- 本運用向けの認証/監査機能は対象外。

構成
- サーバー: Node/Express
- 保存: SQLite（ローカルファイル）
- UI: 静的HTML

起動手順
1) `cd receiver-ui`
2) `npm install`
3) `PORT=5001 npm run dev`
4) ブラウザで `http://localhost:5001/viewer` を開く

端末側設定
- 送信先IP: ローカルPCのIP
- ポート: `5001`
- UI上部に表示される「PCのIP」を使う

エンドポイント
- `POST /api/Products/delivery`
  - 受信したJSONをSQLiteに保存
  - `product_id` と `amount` が欠けている場合は `400` を返す
- `GET /api/Products/delivery/logs?limit=200`
  - 受信ログ一覧を返す
- `GET /api/info`
  - `host_ip` と `port` を返す
- `GET /api/health`
  - ヘルスチェック（200）

UIの表示項目
- 受信時刻（received_at）
- 商品ID（product_id）
- 数量（amount）
- 個体識別番号（individual_id）
- 端末ID（terminal_id）
- 送信時刻（sent_at）
- 受信結果（status）
- メッセージ（message）

補足
- 受信ログは `receiver-ui/data/receiver.db` に保存される。
- 検証用の簡易UIのため、長期保管やアクセス制御は想定していない。

## 便利機能（デバッグ向け）
- 画面右上の「自動更新」トグルで、5秒間隔の自動更新をオン/オフできる。
