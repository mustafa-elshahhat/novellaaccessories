<#
.SYNOPSIS
  Starts the full Novella local development stack, each app in its own window.

.DESCRIPTION
  Launch order (matches service dependencies):
    1. apps/whatsapp   -> http://localhost:4000   (Express + Baileys sidecar; MongoDB)
    2. apps/api        -> http://localhost:5048   (ASP.NET Core; SQL Server; auto-migrate + seed)
    3. apps/storefront -> http://localhost:3000   (Next.js; BFF to the API)
    4. apps/admin      -> http://localhost:5173   (React + Vite SPA)

  This script contains NO secrets. Each app reads its own local configuration:
    - apps/api       : .NET user-secrets (id "novella-api-dev-secrets") + appsettings.Development.json
    - apps/whatsapp  : apps/whatsapp/.env            (git-ignored)
    - apps/storefront: apps/storefront/.env.local    (git-ignored)
    - apps/admin     : apps/admin/.env.local         (git-ignored)

  Prerequisite: SQL Server (e.g. the local SQLEXPRESS instance) must be running first.

.EXAMPLE
  pwsh ./scripts/start-dev.ps1
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Start-App {
    param([string]$Title, [string]$WorkingDir, [string]$Command)
    $full = Join-Path $root $WorkingDir
    if (-not (Test-Path $full)) { throw "Missing app directory: $full" }
    Write-Host "Starting $Title  ($WorkingDir)" -ForegroundColor Cyan
    Start-Process powershell -ArgumentList @(
        '-NoExit', '-Command',
        "`$Host.UI.RawUI.WindowTitle='$Title'; Set-Location '$full'; $Command"
    ) | Out-Null
}

Write-Host "Ensure SQL Server (local SQLEXPRESS / LocalDB) is running before continuing." -ForegroundColor Yellow

Start-App -Title 'Novella WhatsApp :4000'   -WorkingDir 'apps/whatsapp'           -Command 'npm start'
Start-App -Title 'Novella API :5048'         -WorkingDir 'apps/api/src/Novella.Api' -Command 'dotnet run --launch-profile http'
Start-App -Title 'Novella Storefront :3000'  -WorkingDir 'apps/storefront'          -Command 'npm run dev'
Start-App -Title 'Novella Admin :5173'        -WorkingDir 'apps/admin'              -Command 'npm run dev'

Write-Host ""
Write-Host "All four apps launched in separate windows:" -ForegroundColor Green
Write-Host "  WhatsApp   http://localhost:4000/health"
Write-Host "  API        http://localhost:5048/health"
Write-Host "  Storefront http://localhost:3000/ar"
Write-Host "  Admin      http://localhost:5173"
