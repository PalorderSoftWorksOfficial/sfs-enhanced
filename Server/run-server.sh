#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXECUTABLE="$ROOT/../dist/server/linux-x64/SFSEnhanced.Server"
CONFIG="${SFS_CONFIG:-$ROOT/server.json}"

if [[ ! -x "$EXECUTABLE" ]]; then
  dotnet run --project "$ROOT/SFSEnhanced.Server.csproj" -- --config "$CONFIG" "$@"
else
  "$EXECUTABLE" --config "$CONFIG" "$@"
fi
