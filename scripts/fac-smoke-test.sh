#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PORT="${SFS_SMOKE_PORT:-17777}"
DATA_DIR="$(mktemp -d)"
SERVER_PID=""

cleanup() {
  if [[ -n "$SERVER_PID" ]] && kill -0 "$SERVER_PID" 2>/dev/null; then
    kill "$SERVER_PID" 2>/dev/null || true
    wait "$SERVER_PID" 2>/dev/null || true
  fi
  rm -rf "$DATA_DIR"
}
trap cleanup EXIT

cd "$ROOT"
dotnet build Shared/SFSEnhanced.Shared.csproj -c Release
dotnet build Server/SFSEnhanced.Server.csproj -c Release
dotnet build TestClient/SFSEnhanced.TestClient.csproj -c Release

dotnet run --project Server/SFSEnhanced.Server.csproj -- --port "$PORT" --data "$DATA_DIR" --name "FAC Smoke Server" >"$DATA_DIR/server.log" 2>&1 &
SERVER_PID="$!"

for _ in {1..100}; do
  if (echo >"/dev/tcp/127.0.0.1/$PORT") 2>/dev/null; then break; fi
  if ! kill -0 "$SERVER_PID" 2>/dev/null; then
    cat "$DATA_DIR/server.log"
    exit 1
  fi
  sleep 0.1
done

if ! (echo >"/dev/tcp/127.0.0.1/$PORT") 2>/dev/null; then
  cat "$DATA_DIR/server.log"
  exit 1
fi

printf 'world FAC Smoke World\nchat FAC smoke test\nquit\n' | dotnet run --project TestClient/SFSEnhanced.TestClient.csproj -- 127.0.0.1 "$PORT" FAC-Smoke

if ! grep -q "FAC Smoke Server" "$DATA_DIR/server.log"; then
  cat "$DATA_DIR/server.log"
  exit 1
fi

echo "FAC smoke test passed"
