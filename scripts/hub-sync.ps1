<#
.SYNOPSIS
  Checks (or copies) the scenario scripts that exist twice: once in a standalone project (02, 03, 04) and once in the hub.

.DESCRIPTION
  The hub holds a copy of each scenario's scripts under vive-steamvr\hub\Assets\Scripts\Scenarios so that one build can switch between
  scenarios. The standalone projects are the master copy. Project 01 has nothing to sync: its scenario (HydroTest) lives in the shared kit
  (vive-steamvr\packages\com.pittbradford.xrtraining), which every project, including the hub, already uses directly.

  Default (check): compares every .cs file of each standalone project's Assets\Scripts with the hub copy and prints SAME / DIFFERENT / MISSING.
  Exit code 0 when nothing has drifted, 1 when something has.
  -Apply: copies the master file over the hub copy for each DIFFERENT or MISSING file (the .meta file is created by Unity when it is missing).
  It never copies into a project and never touches scenes. After -Apply, rebuild and bake the hub scenes (docs\BUILD-THE-HUB.md); a scene
  made from the old script does not change by itself.

  Known differences that are deliberate are listed in $Exempt below and reported as EXEMPT, not as drift.
  Do not run -Apply while the hub is open in a Unity editor (it recompiles on every file change).

.EXAMPLE
  powershell -File scripts\hub-sync.ps1            # check only
  powershell -File scripts\hub-sync.ps1 -Apply     # copy master files into the hub
#>
[CmdletBinding()]
param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$vive = Join-Path $root 'vive-steamvr'
$hubDir = Join-Path $vive 'hub\Assets\Scripts\Scenarios'
$projects = '02-refinery-rounds', '03-warehouse-forklift', '04-resistor-bench'

# File name -> why the hub copy differs on purpose (or is not needed there)
$Exempt = @{
  'Forklift.cs'     = 'project 03 swaps the primitive driver for a seated Rocketbox avatar (SeatedDriver, PeopleBoot); the hub copy has not been given that and keeps the primitive driver (a known difference, not ported yet)'
  'PeopleBoot.cs'   = 'project 03 only (loads Rocketbox people); not in the hub'
  'SeatedDriver.cs' = 'project 03 only (seated Rocketbox driver)'
}

if ($Apply) {
  $open = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -and $_.CommandLine -match 'vive-steamvr.hub' -and $_.CommandLine -notmatch 'AssetImportWorker' }
  if ($open) { Write-Host 'The hub is open in a Unity editor (process' $open.ProcessId '). Close it first, then run -Apply again.' -ForegroundColor Yellow; exit 2 }
}

$drift = 0
foreach ($p in $projects) {
  $src = Join-Path $vive "$p\Assets\Scripts"
  if (-not (Test-Path $src)) { continue }
  Write-Host "== $p" -ForegroundColor Cyan
  foreach ($f in Get-ChildItem $src -Filter *.cs -File) {
    $dst = Join-Path $hubDir $f.Name
    if ($Exempt.ContainsKey($f.Name)) { Write-Host ("  EXEMPT     {0}  ({1})" -f $f.Name, $Exempt[$f.Name]); continue }
    if (-not (Test-Path $dst)) { $state = 'MISSING' }
    elseif ((Get-FileHash $f.FullName).Hash -eq (Get-FileHash $dst).Hash) { $state = 'SAME' }
    else { $state = 'DIFFERENT' }
    if ($state -eq 'SAME') { Write-Host ("  SAME       {0}" -f $f.Name); continue }
    $drift++
    if ($Apply) { Copy-Item $f.FullName $dst -Force; Write-Host ("  COPIED     {0}  (was {1})" -f $f.Name, $state) -ForegroundColor Green }
    else { Write-Host ("  {0,-10} {1}" -f $state, $f.Name) -ForegroundColor Yellow }
  }
}
if ($drift -eq 0) { Write-Host 'No drift: the hub copies match the standalone projects.' -ForegroundColor Green; exit 0 }
if ($Apply) { Write-Host "$drift file(s) copied. Rebuild and bake the hub scenes (docs\BUILD-THE-HUB.md)." -ForegroundColor Green; exit 0 }
Write-Host "$drift file(s) have drifted. Run with -Apply (hub editor closed) to copy the standalone versions into the hub." -ForegroundColor Yellow
exit 1
