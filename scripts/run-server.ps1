param(
    [string]$Config = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Server = Join-Path $Root "publish\win-x64\SFSEnhanced.Server.exe"
if ([string]::IsNullOrWhiteSpace($Config)) {
    $Config = Join-Path $Root "Server\server.json"
}

if (-not (Test-Path $Server)) {
    throw "Server binary not found: $Server. Run scripts\publish-server.ps1 first."
}

& $Server --config $Config
exit $LASTEXITCODE
