#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT="${1:-$ROOT/publish/linux-x64}"

rm -rf "$OUTPUT"
mkdir -p "$OUTPUT"

dotnet publish "$ROOT/Server/SFSEnhanced.Server.csproj" \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUTPUT"

printf 'Linux server published to %s\n' "$OUTPUT"
printf 'Run: %s/SFSEnhanced.Server --config %s/server.json\n' "$OUTPUT" "$OUTPUT"
