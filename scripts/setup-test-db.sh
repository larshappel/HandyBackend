#!/bin/bash
set -euo pipefail

# MySQL に接続できるようになっていることを前提としています。
# root パスワードが不明な場合は mysql コマンドを別途取得してから実行してください。

MYSQL_HOST=${MYSQL_HOST:-localhost}
MYSQL_PORT=${MYSQL_PORT:-3306}
MYSQL_USER=${MYSQL_USER:-root}
MYSQL_PASS=${MYSQL_PASS:-Givetake}

echo "> 接続: ${MYSQL_USER}@${MYSQL_HOST}:${MYSQL_PORT}"

mysql -h"${MYSQL_HOST}" -P"${MYSQL_PORT}" -u"${MYSQL_USER}" -p"${MYSQL_PASS}" <<'SQL'
CREATE DATABASE IF NOT EXISTS handybackend_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'handytester'@'localhost' IDENTIFIED BY 'TestPass123!';
GRANT ALL PRIVILEGES ON handybackend_dev.* TO 'handytester'@'localhost';
FLUSH PRIVILEGES;
SQL

echo "> handybackend_dev/handytester を作成しました。"
