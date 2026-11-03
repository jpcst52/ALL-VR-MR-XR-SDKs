<#
  Checks everything the lab-PC demo needs and says what to fix. Changes nothing.
  Usage: scripts\demo-doctor.ps1   (also: double-click scripts\demo-doctor.cmd)
#>
param([int]$Port = 8443)

$repo = Split-Path -Parent $PSScriptRoot
$mp = Join-Path $repo 'multi-player'
$bad = 0
function Ok($m)   { Write-Host "  OK    $m" -ForegroundColor Green }
function Warn($m) { Write-Host "  WARN  $m" -ForegroundColor Yellow }
function Fail($m) { Write-Host "  FAIL  $m" -ForegroundColor Red; $script:bad++ }

Write-Host 'Node.js'
$node = Get-Command node -ErrorAction SilentlyContinue
if ($node) { Ok ("node " + (& node -v)) } else { Fail 'Node.js not found. Install the LTS version from https://nodejs.org' }

Write-Host 'Dependencies'
if (Test-Path (Join-Path $mp 'node_modules')) { Ok 'multi-player\node_modules is installed' } else { Fail 'Run npm install in the multi-player folder once (needs internet).' }

Write-Host "Ports $Port to $($Port + 9)"
for ($p = $Port; $p -lt $Port + 10; $p++) {
  $c = Get-NetTCPConnection -LocalPort $p -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
  if (-not $c) { if ($p -eq $Port) { Ok "port $p is free" }; continue }
  $proc = Get-Process -Id $c.OwningProcess -ErrorAction SilentlyContinue
  & node (Join-Path $PSScriptRoot 'relay-probe.mjs') $p *> $null
  $isRelay = ($LASTEXITCODE -eq 0)
  if ($isRelay) { Ok "port ${p}: this demo is running ($($proc.ProcessName), PID $($c.OwningProcess)). Stop it with scripts\stop-demo.ps1" }
  else { Warn "port ${p} is used by $($proc.ProcessName) (PID $($c.OwningProcess)), not this demo. start-demo moves to the next free port by itself." }
}

Write-Host 'Network'
$ips = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue | Where-Object { $_.IPAddress -notmatch '^(127\.|169\.254\.)' -and $_.PrefixOrigin -ne 'WellKnown' }
if ($ips) { foreach ($i in $ips) { Ok "$($i.InterfaceAlias): $($i.IPAddress)" } } else { Warn 'No LAN address found: headsets cannot reach this PC.' }
$profiles = Get-NetConnectionProfile -ErrorAction SilentlyContinue
foreach ($pr in $profiles) { if ($pr.NetworkCategory -eq 'Public') { Warn "Network '$($pr.Name)' is set to Public: Windows blocks incoming connections. Set it to Private (Settings > Network)." } }

Write-Host 'Windows Firewall'
$rules = Get-NetFirewallRule -Direction Inbound -Enabled True -Action Allow -ErrorAction SilentlyContinue |
  Where-Object { $_.DisplayName -match 'Node' } | Select-Object -First 3
if ($rules) { Ok ("an inbound rule for Node exists: " + (($rules | ForEach-Object DisplayName) -join ', ')) }
else { Warn "No inbound 'Node.js' rule. The first time the server starts, Windows asks to allow it: choose Private networks. Headsets cannot connect until you do." }

Write-Host 'Certificate'
$cert = Join-Path $mp '.certs'
if (Test-Path $cert) { Ok "a cached self-signed certificate exists in multi-player\.certs (it is regenerated when the PC's addresses change)" }
else { Ok 'no certificate yet: it is created on first start (headsets then show a one-time warning: Advanced, Proceed)' }

Write-Host ''
if ($bad) { Write-Host "$bad problem(s) to fix." -ForegroundColor Red; exit 1 } else { Write-Host 'Ready: run scripts\start-demo.cmd' -ForegroundColor Green }
