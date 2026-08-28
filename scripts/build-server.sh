#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT="${1:-$ROOT/dist/server/linux-x64}"

cd "$ROOT"
dotnet restore SFSEnhanced.sln
dotnet publish Server/SFSEnhanced.Server.csproj -c Release -r linux-x64 --self-contained true -o "$OUTPUT"

printf 'Server published to %s\n' "$OUTPUT"
