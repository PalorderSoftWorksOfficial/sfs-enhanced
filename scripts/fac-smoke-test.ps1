$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Port = if ($env:SFS_SMOKE_PORT) { [int]$env:SFS_SMOKE_PORT } else { 17777 }
$DataDir = Join-Path ([System.IO.Path]::GetTempPath()) ("sfs-enhanced-smoke-" + [guid]::NewGuid().ToString("N"))
$ServerProcess = $null
try {
    New-Item -ItemType Directory -Path $DataDir -Force | Out-Null
    Set-Location $Root
    dotnet build Shared\SFSEnhanced.Shared.csproj -c Release
    dotnet build Server\SFSEnhanced.Server.csproj -c Release
    dotnet build TestClient\SFSEnhanced.TestClient.csproj -c Release
    $ServerLog = Join-Path $DataDir "server.log"
    $ServerErrorLog = Join-Path $DataDir "server-error.log"
    $ServerProcess = Start-Process dotnet -ArgumentList @("run", "--project", "Server\SFSEnhanced.Server.csproj", "--", "--port", $Port, "--data", $DataDir, "--name", "FAC Smoke Server") -RedirectStandardOutput $ServerLog -RedirectStandardError $ServerErrorLog -PassThru
    Start-Sleep -Seconds 2
    "world FAC Smoke World`nchat FAC smoke test`nquit" | dotnet run --project TestClient\SFSEnhanced.TestClient.csproj -- 127.0.0.1 $Port FAC-Smoke
    if (-not (Select-String -Path $ServerLog -Pattern "FAC Smoke Server" -Quiet)) {
        Get-Content $ServerLog
        Get-Content $ServerErrorLog
        throw "Server smoke test did not report the expected server name."
    }
    Write-Host "FAC smoke test passed"
}
finally {
    if ($null -ne $ServerProcess -and -not $ServerProcess.HasExited) {
        $ServerProcess.Kill()
        $ServerProcess.WaitForExit()
    }
    if (Test-Path $DataDir) { Remove-Item $DataDir -Recurse -Force }
}
