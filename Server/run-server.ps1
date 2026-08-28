$ErrorActionPreference = "Stop"

$Root = $PSScriptRoot
$Executable = Join-Path $Root "..\dist\server\win-x64\SFSEnhanced.Server.exe"
$Config = if ($env:SFS_CONFIG) { $env:SFS_CONFIG } else { Join-Path $Root "server.json" }

if (Test-Path $Executable) {
    & $Executable --config $Config @args
} else {
    dotnet run --project (Join-Path $Root "SFSEnhanced.Server.csproj") -- --config $Config @args
}
