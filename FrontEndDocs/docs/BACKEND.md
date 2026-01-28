# バックエンドのあるべき姿（本番）
# Backend expected behavior (production)

## 1. 役割とデータフロー / Roles and data flow

### 1.1 Handy → Backend（取り込み） / Handy → Backend (ingest)
- ハンディ端末から納品データをPOSTで送信する。
- Backendは受信データを検証し、DBに保存する。
- Backendは成功/失敗をHTTPで返す。

### 1.2 Backend → Magic xpa（連携） / Backend → Magic xpa (integration)
- Magic xpa が Backend から納品データを取得する想定。
- 方式は未確定（Pull/Pushのどちらか）。
  - Pull: Magic xpa が GET で取得する。
  - Push: Backend が Magic xpa に送信する。

## 2. 正常系 / Normal flow

### 2.1 取り込み / Ingest
- エンドポイント: `POST /api/Products/delivery`
- リクエスト例:
  - `product_id`（必須）
  - `amount`（必須）
  - `individual_id`（任意）
  - `terminal_id`（任意）
- `sent_at`（任意、ISO-8601）
- Backendの挙動:
  - 必須項目の検証
  - 受信時刻 `received_at` を付与してDB保存
  - `200` と `{ success: true, message: "accepted" }` を返す

### 2.2 Magic xpa 連携 / Magic xpa integration
- Backendは受理済みデータを提供する。
- Pullの場合:
  - `GET /api/Products/delivery/logs?limit=...`
  - 追加候補: `since`, `status`, `terminal_id`
- Pushの場合:
  - BackendがMagic xpaのAPIへ送信し、送達済みをマークする。
- Backendはステータスを保持する（例: `PENDING`, `DELIVERED`, `FAILED`）。

## 3. 異常系 / Abnormal flow

### 3.1 バリデーションエラー / Validation errors
- `product_id` / `amount` 欠如 → `400`
- 形式不正 → `400`
- 返却例: `{ success: false, message: "Validation failed" }`
- 不正データは保存しない。

### 3.2 Backend内部エラー / Backend internal errors
- DBエラー → `500`
- 予期しない例外 → `500`
- リクエスト情報とともにエラーログを残す。

### 3.3 Magic xpa連携エラー / Magic xpa integration errors
- Magic xpa停止/到達不可 → 再送または `FAILED` に遷移
- 部分失敗 → `PENDING` を保持して再送対象
- 再送/再エクスポート手段を用意する。

## 4. Handy側との整合 / Alignment with Handy app

Handyアプリの扱い（現状） / Current Handy behavior:
- 200/204: 成功
- 400/403/404: 恒久エラー扱い（バッファしない）
- 5xx/通信失敗: バッファ再送

Backend側の推奨 / Backend guidance:
- `400/403/404` は恒久エラー（入力不正/対象なし）のみで使う
- 一時的な障害は `5xx` を返し、再送対象にする

## 5. 未確定事項 / Open questions

- Magic xpa 連携方式（Pull/Push）
- Magic xpa に必要な必須項目
- 重複排除（idempotency）方針
- 取り込み後の可視化/反映のSLA
