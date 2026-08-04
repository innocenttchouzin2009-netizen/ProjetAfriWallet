#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"

curl -sS -X POST "$BASE_URL/api/v1/wallets" -H 'Content-Type: application/json' -d '{"Awid":"demo-awid","WalletType":"Personal","Currency":"EUR"}' | jq '.'

echo "Demo flow ready."
