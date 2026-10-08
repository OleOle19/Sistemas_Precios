$ErrorActionPreference = 'Stop'
$taskRecord = Join-Path (Split-Path -Parent $PSScriptRoot) 'services/api/storage/runtime-processes.json'
if (-not (Test-Path -LiteralPath $taskRecord)) { Write-Host 'No hay una ejecución registrada.'; return }
foreach ($taskEntry in (Get-Content -LiteralPath $taskRecord -Raw | ConvertFrom-Json)) {
    $taskProcess = Get-Process -Id $taskEntry.Id -ErrorAction SilentlyContinue
    if ($taskProcess -and $taskProcess.StartTime.ToUniversalTime().ToString('o') -eq $taskEntry.Started -and $taskProcess.ProcessName -in @('dotnet','cmd')) {
        taskkill.exe /PID $taskEntry.Id /T /F | Out-Null
    }
}
Write-Host 'Ejecución local detenida.'
