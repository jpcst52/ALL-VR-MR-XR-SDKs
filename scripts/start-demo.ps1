<#
  One-action start for the lab-PC demo.
  Starts the HTTPS relay + static site (port 8443), opens the launcher, prints the Quest and Vive steps.
  Optional: -Dashboard also starts the status dashboard (the address it prints (a free port is chosen automatically)).
  Optional: -Port 8443 to change the port.
#>
param(
  [switch]$Dashboard,
  [int]$Port = 8443
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$mp = Join-Path $repo 'multi-player'

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
  Write-Host 'Node.js was not found. Install the LTS version from https://nodejs.org and run this again.' -ForegroundColor Red
  exit 1
}
Write-Host ("Using Node " + (& node -v))

if (-not (Test-Path (Join-Path $mp 'node_modules'))) {
  Write-Host 'First run: installing dependencies (needs internet once)...'
  Push-Location $mp
  try { & npm install } finally { Pop-Location }
  if ($LASTEXITCODE -ne 0) { Write-Host 'npm install failed.' -ForegroundColor Red; exit 1 }
}

$busy = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if ($busy) {
  Write-Host "Port $Port is already in use (PID $($busy[0].OwningProcess)). Close that program or run with -Port <other>." -ForegroundColor Red
  exit 1
}

if ($Dashboard) {
  $dash = Join-Path $repo 'tools\status\server.cjs'
  if (Test-Path $dash) {
    Start-Process -WindowStyle Minimized -FilePath node -ArgumentList "`"$dash`"" -WorkingDirectory $repo | Out-Null
    Write-Host 'Status dashboard: the address it prints (a free port is chosen automatically)'
  } else {
    Write-Host 'Dashboard script not found, skipping.' -ForegroundColor Yellow
  }
}

Write-Host @"

=== Quest (each headset) ===
 1. Same Wi-Fi as this PC. In Quest Browser open one of the https:// addresses printed below
    (or scan the codes at https://localhost:$Port/multi-player/qr.html).
 2. First visit: accept the certificate warning (Advanced, then Proceed).
 3. Enter a name and the shared room name, then Enter VR.

=== Vive Pro 2 (this PC) ===
 1. Start SteamVR and confirm the headset is tracked (verify at rehearsal).
 2. Open the same page in desktop Chrome/Edge, or press Play in the Unity project.

Close this window (or press Ctrl+C) to stop the server.
"@

# Open the launcher once the server answers.
$null = Start-Job -ScriptBlock {
  param($p)
  for ($i = 0; $i -lt 40; $i++) {
    if (Get-NetTCPConnection -LocalPort $p -State Listen -ErrorAction SilentlyContinue) {
      Start-Process "https://localhost:$p/"
      return
    }
    Start-Sleep -Milliseconds 500
  }
} -ArgumentList $Port

$env:PORT = "$Port"
Push-Location $mp
try { & node server.mjs } finally { Pop-Location }
