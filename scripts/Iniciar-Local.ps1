$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path -Parent $PSScriptRoot
$taskStorage = Join-Path $taskRepo 'services/api/storage'
New-Item -ItemType Directory -Force -Path $taskStorage | Out-Null
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue) -or -not (Get-Command npm.cmd -ErrorAction SilentlyContinue)) {
    throw 'Instala .NET SDK 9 y Node.js 22 o posterior antes de iniciar.'
}
if (Get-NetTCPConnection -State Listen -LocalPort 8080,3000 -ErrorAction SilentlyContinue) {
    throw 'Los puertos 8080 o 3000 ya están ocupados. Cierra la ejecución anterior antes de iniciar otra.'
}
if (-not (Test-Path -LiteralPath (Join-Path $taskStorage 'local-secrets.json')) -and -not $env:Bootstrap__Password) {
    & (Join-Path $PSScriptRoot 'Configurar-Local.ps1')
    if (-not (Test-Path -LiteralPath (Join-Path $taskStorage 'local-secrets.json'))) { return }
}
Push-Location $taskRepo
try {
    if (-not (Test-Path -LiteralPath 'node_modules')) { npm.cmd ci; if ($LASTEXITCODE -ne 0) { throw 'No se pudieron instalar las dependencias.' } }
    if (-not (Test-Path -LiteralPath 'apps/web/.next/BUILD_ID')) { npm.cmd run build:web; if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar la web.' } }
    $taskApi = Start-Process dotnet -ArgumentList @('run','--project','services/api','--no-launch-profile','--urls','http://localhost:8080') -WorkingDirectory $taskRepo -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskStorage 'api-out.log') -RedirectStandardError (Join-Path $taskStorage 'api-error.log')
    $taskWeb = Start-Process cmd.exe -ArgumentList @('/d','/c','npm --workspace web run start') -WorkingDirectory $taskRepo -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskStorage 'web-out.log') -RedirectStandardError (Join-Path $taskStorage 'web-error.log')
    @(@{Id=$taskApi.Id;Started=$taskApi.StartTime.ToUniversalTime().ToString('o')},@{Id=$taskWeb.Id;Started=$taskWeb.StartTime.ToUniversalTime().ToString('o')}) | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskStorage 'runtime-processes.json') -Encoding UTF8
    Write-Host 'Sistema iniciado. Abre http://localhost:3000. Para cerrar, ejecuta scripts/Detener-Local.ps1.'
} finally { Pop-Location }
