# 不具合レポート: 連続スキャン時の入力消失

## 1. 現象と影響

- 現象: 連続スキャン時に「個体識別ID」などの入力値が一瞬表示された後に消える。
- 影響: 個体識別IDが欠落したまま送信されるリスクがある。

## 2. 仕様再確認 (現行実装ベース)

- 商品ID: 12桁固定（先頭`9`）。13桁で先頭`9`の場合は先頭1文字を削除して12桁化。上記以外はエラー。
- 個体識別ID: 10桁数字なら採用。先頭が `251` の場合は直後10桁を抽出（後続が長くても可）。該当しない場合はエラー表示（未入力扱い）。
- 数量: 手入力時は小数点必須。数量バーコードに商品IDが混入した場合はエラー扱い。
- フォーカス: F1〜F3で移動可能。フォーカス取得時は上書きガードが有効になる。
  - Enterは商品ID+数量が入っていれば送信（フォーカス不問）
  - F4は無効

## 3. 原因の最有力候補 (現行コード調査結果)

最有力は「フォーカス取得時のクリア処理」と「スキャン入力イベントの順序競合」です。

根拠:
- フォーカス取得時の上書きガード/復元処理があり、
  高速スキャン時に「入力→フォーカスイベント→復元」が逆順で走ると値が消える可能性がある。
  - 対象: `app/src/main/java/com/example/handytestapp/ui/components/IndividualIdTextField.kt`
  - 同様の挙動が商品ID/数量にも存在。
- フォーカス喪失時に `extractId` を適用し、該当しない場合は空文字列になるため、
  入力欠落やイベント欠損があると「消えたように見える」。
- `DeliveryEntryScreen` の `onPreviewKeyEvent` が数字キーを消費しており、
  スキャナ入力がキーイベントの場合は入力欠落を引き起こす可能性がある。
  - 対象: `app/src/main/java/com/example/handytestapp/ui/screens/DeliveryEntryScreen.kt`

結論: 「フォーカス取得時クリア」単独でも消失が起き得るが、
「スキャン/Tabイベントの順序」と「キーイベント消費」が重なると再現性が高まる。

## 4. 本当に最有力かを確定するための確認案

1. フォーカス取得時のクリアを一時的に無効化し、消失が止まるか確認。
2. `onPreviewKeyEvent` の数字キー消費を一時停止し、入力欠落が消えるか確認。
3. 入力イベント順序の可視化ログを追加し、
   「フォーカス取得→クリア→入力」または「入力→フォーカス取得→クリア」
   のどちらが再現時に起きているかを記録する。

## 5. ログ実装案

目的: 連続スキャン時のイベント順序と値の変化を特定する。

追加ログ候補:
- 各入力欄で
  - フォーカス取得/喪失
  - `onValueChange` の受信値
  - フィールドに設定された最終値
- `DeliveryEntryScreen` で
  - `onPreviewKeyEvent` / `onKeyEvent` のキーコード
  - フォーカス位置 (`focusPosition`) の変更

ログ出力例 (タグは `ScanDebug` などで統一):
- `ScanDebug: IndividualId focus gained`
- `ScanDebug: IndividualId onValueChange=...`
- `ScanDebug: IndividualId focus lost -> extractId=...`
- `ScanDebug: PreviewKey keyCode=... focusPosition=...`

このログにより「入力が消える直前のイベント順序」を追跡可能。

## 6. 現在ログが出ているかの確認結果

既に `Log.d` は多数あり (デバッグ用)。ただし、
- フォーカス取得時のクリア実行タイミング
- 連続スキャン時の `onValueChange` とフォーカスイベントの順序
を特定できるログは不足。

確認済みログ箇所:
- `app/src/main/java/com/example/handytestapp/MainActivity.kt` (キー押下ログ)
- `app/src/main/java/com/example/handytestapp/ui/components/*TextField.kt` (フォーカス喪失ログ)
- `app/src/main/java/com/example/handytestapp/ui/screens/DeliveryEntryScreen.kt` (入力・フォーカス関連ログ)

結論: ログはあるが「消失バグの原因特定には足りない」。
新規ログ追加が必要。

## 6.1 追加したログと見方

追加ログの目的: 連続スキャン時にフォーカスイベントと入力がどの順で起きたかを追跡する。

ログタグ:
- `ScanDebug`

出力箇所:
- `app/src/main/java/com/example/handytestapp/ui/components/ProductIdTextField.kt`
  - フォーカス取得/喪失、フォーカスイベントでのキーボード制御
- `app/src/main/java/com/example/handytestapp/ui/components/AmountTextField.kt`
  - フォーカス取得/喪失、フォーカスイベントでのキーボード制御
- `app/src/main/java/com/example/handytestapp/ui/components/IndividualIdTextField.kt`
  - フォーカス取得/喪失、フォーカスイベントでのキーボード制御

ログの見方:
- 「focus gained」→「focus event」→「focus lost」の順になれば正常な流れ
- 高速スキャン時に「focus lost」の後に「focus event」が出ていないかを確認する
  - もし「focus event」が遅れて出ている場合は、入力クリアとの競合が起きている可能性がある

## 7. 修正計画 (更新版・0.5秒間隔対応)

1. ログ強化でイベント順序を可視化
   - フォーカス取得/喪失と `onValueChange` の順序を記録し、再現時の時系列を特定する。
2. フォーカス取得時の無条件クリアを廃止
   - 取得時はクリアせず、必要なら「全選択」で上書き入力を促す。
3. スキャン開始検知でのみ対象フィールドをクリア
   - スキャン入力の開始イベントをトリガにその欄だけクリアし、追記を防止する。
4. 入力完了検知後のみ送信可能にする
   - Tab/Enter/規定桁数などで入力完了とみなし、その時点で送信可能にする。
5. `onPreviewKeyEvent` の入力消費を最小化
   - 数字キーの消費を外す、またはフォーカス移動専用キーのみに限定する。
6. 回帰確認の手順化
  - 0.5秒間隔での連続スキャンを `docs/TEST.md` に明記し、毎回検証できるようにする。

## 8. まだ残る課題

- スキャナ入力の「開始/終了」を確実に検知する方法が必要
  - 端末側で Tab/Enter が自動付与される前提か確認が必要
- 送信ボタンの有効化条件と入力完了条件の整合性
  - 途中入力で送信可能にならないようにする必要がある
- 二重送信防止の明示的な要件定義
  - UIだけで防ぐのか、送信側で重複排除するのかの方針が必要

## 9. クリップボードでは解決できない理由

- 問題の本質が「フォーカス取得時のクリア」と「入力イベント順序の競合」にあるため、
  クリップボードに一時保存しても直後のクリア処理で上書きされる可能性がある
- クリップボードの操作はOSやIMEの挙動に依存し、スキャナ入力の順序保証にならない
- 高速スキャンではイベントが密に発生するため、クリップボードの読み書きが新たな遅延要因になり得る
