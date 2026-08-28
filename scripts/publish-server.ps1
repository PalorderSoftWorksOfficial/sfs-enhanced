param(
    [string]$Output = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $Root "publish\win-x64"
}

if (Test-Path $Output) {
    Remove-Item $Output -Recurse -Force
}
New-Item -ItemType Directory -Path $Output -Force | Out-Null

dotnet publish (Join-Path $Root "Server\SFSEnhanced.Server.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $Output

Write-Host "Windows server published to $Output"
Write-Host "Run: $Output\SFSEnhanced.Server.exe --config $Output\server.json"
