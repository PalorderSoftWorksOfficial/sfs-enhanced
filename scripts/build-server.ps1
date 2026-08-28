$ErrorActionPreference = "Stop"

$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Output = if ($args.Count -gt 0) { $args[0] } else { Join-Path $Root "dist\server\win-x64" }

Set-Location $Root
dotnet restore SFSEnhanced.sln
dotnet publish Server\SFSEnhanced.Server.csproj -c Release -r win-x64 --self-contained true -o $Output

Write-Host "Server published to $Output"
