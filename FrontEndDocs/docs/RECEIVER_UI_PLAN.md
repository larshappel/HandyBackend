# 受け側UI 計画書・UI設計書（ローカルPC向け）

手順（起動と端末IP設定）
1. 受け側サービスをローカルPCで起動する。
   - 例: `npm install` → `npm run dev`
2. ブラウザで `http://localhost:<ポート>/viewer` を開く。
3. UI上に表示される「PCのIP」を確認する。
4. 端末側の送信先IP/ポートに、手順3のIPを設定する。
5. 端末アプリから送信し、受け側UIで受信ログを確認する。

目的
- ハンディ端末から送信された納品データを、人間がローカルPCで目視確認できるUIを用意する。
- 認証は行わず、手動更新で最新の受信状況を確認できることを優先する。

前提
- 受信APIは `POST /api/Products/delivery` を想定。
- リクエスト形式は `docs/DELIVERY_API.md` に準拠。
- ローカルPCで完結（開発端末で起動）。

対象データ（表示必須）
- 個体識別番号（individual_id）
- 商品ID（product_id）
- 数量（amount）
- 端末ID（terminal_id）
- 送信時刻（sent_at）
- 受信時刻（サーバー側で付与）
- 受信結果（OK/NG、エラーメッセージ）

最小構成（実装の簡単さ優先）
- 受信APIと同一プロセス内に、簡易Web UIを用意する。
- UIは静的HTML + 最小JSで一覧取得・手動更新。
- 保管先は SQLite（ローカルファイル）を使用する。

計画
1. 受信APIに記録機能を追加
   - 受信内容と受信時刻、結果をログに保存。
   - 保存先は SQLite（ローカルファイル）。
2. 一覧取得エンドポイントの追加
   - `GET /api/Products/delivery/logs` を追加。
   - 直近N件を返す（例: 200件）。
3. 情報取得エンドポイントを追加
   - `GET /api/info` を追加（サーバーが認識しているPCのIP/ポートを返す）。
4. 簡易Web UIを追加
   - `/viewer` にHTMLを配置。
   - 「更新」ボタンで `GET /api/Products/delivery/logs` を取得。
   - 画面上部にPCのIP表示エリアを設置し、`GET /api/info` で埋める。
5. 表示・動作確認
   - 手動更新で最新データが見えること。
   - 必須項目が欠けていないこと。

UI設計（最小）
画面: 受信ログビューア

レイアウト
- ヘッダー: タイトル + 最終更新時刻 + 更新ボタン + PCのIP表示
- メイン: 受信ログのテーブル

操作
- 更新ボタン: 最新ログを再取得して一覧更新
- 受信件数表示: 直近の取得件数

表示項目（テーブル列）
- 受信時刻（received_at）
- 商品ID（product_id）
- 数量（amount）
- 個体識別番号（individual_id）
- 端末ID（terminal_id）
- 送信時刻（sent_at）
- 受信結果（status、OK/NG）
- メッセージ（message、エラー理由など）

表示ルール
- OKは緑、NGは赤のラベル表示（色は最小限で可）
- `terminal_id` が空の場合は "(empty)" 表記

API設計（追加分）
- `GET /api/Products/delivery/logs`
  - クエリ: `limit`（任意、デフォルト 200）
  - レスポンス例:
  ```json
  {
    "items": [
      {
        "received_at": "2026-01-21T08:47:12.516Z",
        "product_id": "001000001579",
        "amount": "0.146",
        "individual_id": "1639895531",
        "terminal_id": "A1-01",
        "sent_at": "2026-01-21T08:47:10.516Z",
        "status": "OK",
        "message": "accepted"
      }
    ]
  }
  ```
- `GET /api/info`
  - レスポンス例:
  ```json
  {
    "host_ip": "192.168.1.10",
    "port": 5000
  }
  ```

実装方針（軽量）
- ローカル用の最小サーバーを新設（Node/Express + SQLite）
- 起動コマンドは `npm install` → `npm run dev` を想定

テスト観点（最小）
- 正常送信: 送信後に UI で1件表示される
- 送信エラー: NGとして表示され、エラーメッセージが出る
- terminal_id 空: 空表記で表示される

運用メモ
- これは検証用の簡易ビューア。長期保管や認証は不要。
- 本番導入は別途検討。
