#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SERVER="$ROOT/publish/linux-x64/SFSEnhanced.Server"
CONFIG="${1:-$ROOT/Server/server.json}"

if [[ ! -x "$SERVER" ]]; then
  echo "Server binary not found: $SERVER" >&2
  echo "Run scripts/publish-server.sh first." >&2
  exit 1
fi

exec "$SERVER" --config "$CONFIG"
