<#
  Stops copies of THIS demo (the relay + menu server) and nothing else.
  It looks at ports 8443 to 8492, asks each listener whether it is the relay (the /healthz answer), and stops only those.
  Other programs on those ports are listed and left alone.
  Usage: scripts\stop-demo.ps1            (stop them)
         scripts\stop-demo.ps1 -WhatIf    (only list what would be stopped)
#>
[CmdletBinding(SupportsShouldProcess)]
param([int]$FirstPort = 8443)

function Test-Relay([int]$p) {
  & node (Join-Path $PSScriptRoot 'relay-probe.mjs') $p *> $null
  return ($LASTEXITCODE -eq 0)
}

$found = 0
for ($p = $FirstPort; $p -lt $FirstPort + 50; $p++) {
  $c = Get-NetTCPConnection -LocalPort $p -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
  if (-not $c) { continue }
  $proc = Get-Process -Id $c.OwningProcess -ErrorAction SilentlyContinue
  if (Test-Relay $p) {
    $found++
    if ($c.OwningProcess -le 4) {
      Write-Host "Port $p is this demo, but Windows does not show which program owns it (started from another session?). Close its window, or restart the PC." -ForegroundColor Yellow
    } elseif ($PSCmdlet.ShouldProcess("port $p ($($proc.ProcessName), PID $($c.OwningProcess))", 'Stop the demo server')) {
      try {
        Stop-Process -Id $c.OwningProcess -Force -ErrorAction Stop
        Write-Host "Stopped the demo on port $p (PID $($c.OwningProcess))." -ForegroundColor Green
      } catch {
        Write-Host "Could not stop PID $($c.OwningProcess) on port ${p}: $($_.Exception.Message)" -ForegroundColor Yellow
      }
    }
  } else {
    Write-Host "Port $p is used by $($proc.ProcessName) (PID $($c.OwningProcess)), not this demo: left alone."
  }
}
if (-not $found) { Write-Host 'No copy of the demo is running.' }
