# Corre los tres niveles de pruebas con una sola orden y no deja nada atrás:
#   1) pruebas de .NET (contrato y sesión, cada una con su base descartable)
#   2) lógica pura del cliente (Node)
#   3) recorrido de punta a punta en Chromium con el servidor apagado y vuelto a encender
# Uso (desde cualquier carpeta):  ./ISFDyT124.Tests/correr-todo.ps1
$ErrorActionPreference = 'Stop'
$tests = $PSScriptRoot
$app = Join-Path $tests '..\ISFDyT124'
$servidor = '(localdb)\MSSQLLocalDB'
$base = 'PwaE2E_' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$resultado = 0
try {
    dotnet test (Join-Path $tests 'ISFDyT124.Tests.csproj'); if ($LASTEXITCODE -ne 0) { $resultado = 1 }

    Push-Location (Join-Path $tests 'cliente')
    node --test logica/humo.test.js logica/pwa-core.test.js logica/sw-lista.test.js; if ($LASTEXITCODE -ne 0) { $resultado = 1 }

    $env:ConnectionStrings__DBSI = "Server=$servidor;Database=$base;Trusted_Connection=True;TrustServerCertificate=True;"
    $env:BASE_E2E = $base
    $env:SEMILLA_SQL = (Resolve-Path (Join-Path $tests 'semilla-pwa.sql')).Path
    dotnet build $app -v q; if ($LASTEXITCODE -ne 0) { throw 'no compila' }
    dotnet ef database update --project $app --no-build --connection $env:ConnectionStrings__DBSI; if ($LASTEXITCODE -ne 0) { throw 'no se pudo crear la base' }
    node e2e/flujo-offline.mjs; if ($LASTEXITCODE -ne 0) { $resultado = 1 }
    Pop-Location
}
finally {
    # Sin procesos ni bases huérfanas.
    Get-NetTCPConnection -LocalPort 5199 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
    sqlcmd -S $servidor -Q "IF DB_ID('$base') IS NOT NULL BEGIN ALTER DATABASE [$base] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$base] END" | Out-Null
    Remove-Item Env:ConnectionStrings__DBSI, Env:BASE_E2E, Env:SEMILLA_SQL -ErrorAction SilentlyContinue
}
if ($resultado -eq 0) { Write-Host 'TODO EN VERDE' -ForegroundColor Green } else { Write-Host 'HAY FALLAS' -ForegroundColor Red }
exit $resultado
