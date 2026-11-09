<#
.SYNOPSIS
  Builds Windows 64-bit standalone players of the Unity training projects for the Vive Pro 2 + SteamVR "VR Stage" PC.

.DESCRIPTION
  For each project it runs Unity once, headless of any open editor, calling the project's own build method
  (ProjectSetup.BuildWindows, which calls the kit's Configurator.BuildWindows) with -quit and -logFile.
  Output goes to builds\vive\<project>\ (the whole folder: the .exe AND its <name>_Data folder, UnityPlayer.dll, etc.;
  copy the whole folder). Each folder gets a README-VR-STAGE.txt and is zipped; a combined zip is written too.

  The repository documents its Unity command-line runs WITHOUT -batchmode (it needs graphics for captures and bakes).
  This script follows that. Add -Batchmode to try a faster, windowless build (not tried here).

  Not run for real by the author of this script (no Unity available to the author): use -DryRun first.

.PARAMETER IncludeHub
  Also build vive-steamvr\hub (needs the local Asset Store packs and Rocketbox; never publish that build).
  The hub builds with HubBuild.BuildWindows (vive-steamvr\hub\Assets\Editor\HubBuild.cs); about 11 minutes and 1 GB.
.PARAMETER Projects
  Build only these project folder names, for example -Projects 01-hydrostatic-test,04-resistor-bench.
.PARAMETER UnityExe
  Path to Unity.exe (6000.3.20f1). Default: $env:UNITY_EXE, else C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe.
.PARAMETER DryRun
  (alias -WhatIf) Print what would run; do not start Unity, write builds or zips.
.PARAMETER NoZip
  Skip the zip files.
.PARAMETER Batchmode
  Pass -batchmode to Unity (untested here).
#>
[CmdletBinding()]
param(
  [switch]$IncludeHub,
  [string[]]$Projects,
  [string]$UnityExe,
  [Alias('WhatIf')][switch]$DryRun,
  [switch]$NoZip,
  [switch]$Batchmode
)

$ErrorActionPreference = 'Stop'
$Repo = Split-Path -Parent $PSScriptRoot
$Vive = Join-Path $Repo 'vive-steamvr'
$OutRoot = Join-Path $Repo 'builds\vive'
$LogDir = Join-Path $Repo 'builds\logs'
$Stamp = Get-Date -Format 'yyyy-MM-dd'

if (-not $UnityExe) { $UnityExe = $env:UNITY_EXE }
if (-not $UnityExe) { $UnityExe = 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe' }

$all = @('01-hydrostatic-test', '02-refinery-rounds', '03-warehouse-forklift', '04-resistor-bench')
if ($IncludeHub) { $all += 'hub' }
if ($Projects) { $all = $all | Where-Object { $Projects -contains $_ } }
if (-not $all) { throw 'No project selected.' }

Write-Host "Repository : $Repo"
Write-Host "Unity      : $UnityExe"
Write-Host "Output     : $OutRoot"
Write-Host "Mode       : $(if ($DryRun) { 'DRY RUN (nothing is built)' } else { 'real build' })"
if (-not (Test-Path -LiteralPath $UnityExe)) {
  if ($DryRun) { Write-Warning "Unity.exe not found at that path (a real run would stop here). Use -UnityExe or set UNITY_EXE." }
  else { throw "Unity.exe not found: $UnityExe. Install Unity 6000.3.20f1 or pass -UnityExe / set UNITY_EXE." }
}

function Get-OpenEditors([string]$projectPath) {
  # Unity processes whose command line mentions the project folder (either slash style).
  $a = $projectPath.TrimEnd('\')
  $b = $a.Replace('\', '/')
  Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -and ($_.CommandLine.IndexOf($a, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or $_.CommandLine.IndexOf($b, [StringComparison]::OrdinalIgnoreCase) -ge 0) }
}

function Get-BuildTarget([string]$projectPath) {
  # Finds  BuildWindows("Builds/<Name>/<Name>.exe")  in the project's Editor scripts.
  $editor = Join-Path $projectPath 'Assets\Editor'
  if (-not (Test-Path $editor)) { return $null }
  foreach ($f in Get-ChildItem $editor -Filter *.cs -File -ErrorAction SilentlyContinue) {
    $m = [regex]::Match((Get-Content -Raw $f.FullName), 'static\s+void\s+(\w+)\s*\(\s*\)\s*=>\s*(?:\w+\.)?Configurator\.BuildWindows\(\s*"([^"]+)"')
    if ($m.Success) {
      $cls = [regex]::Match((Get-Content -Raw $f.FullName), 'class\s+(\w+)').Groups[1].Value
      return [pscustomobject]@{ Method = "$cls.$($m.Groups[1].Value)"; Relative = $m.Groups[2].Value }
    }
  }
  return $null
}

function Test-HubAssets([string]$hubPath) {
  # The hub's scenes and art packs are licensed and git-ignored, so a fresh clone cannot build it. Returns the missing items.
  $need = @('Assets\Scenes\Local\Lobby.unity', 'Assets\Scenes\Local\HydrostaticTest_Hub.unity', 'Assets\Scenes\Local\RefineryRounds_Hub.unity',
            'Assets\Scenes\Local\WarehouseForklift_Hub.unity', 'Assets\Scenes\Local\ResistorBench_Hub.unity', 'Assets\Rocketbox', 'Assets\Baked')
  $need | Where-Object { -not (Test-Path (Join-Path $hubPath $_)) }
}

function Get-FolderMB([string]$p) {
  if (-not (Test-Path $p)) { return 0 }
  [math]::Round(((Get-ChildItem $p -Recurse -File -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum) / 1MB, 1)
}

function New-StageReadme([string]$title, [string]$exe, [bool]$isHub) {
@"
$title - Windows build for the VR Stage PC
Built $Stamp from the XR training repository (Unity 6000.3.20f1, OpenXR, Windows 64-bit).

NOT YET TESTED ON THE VR STAGE PC OR WITH A HEADSET. This folder was produced by scripts\build-vive.ps1.
Treat the first run as a test and note what you see.

Copy the WHOLE folder (this README, $exe, the *_Data folder, UnityPlayer.dll and the rest) to the VR Stage PC,
for example to C:\XRTraining\. The .exe does not run on its own.

Install on the VR Stage PC (once)
  1. Steam, then SteamVR (Steam Library > Tools > SteamVR). Set up the HTC Vive Pro 2 (wireless adapter or link box)
     and check the room set-up in SteamVR.
  2. In SteamVR: Settings > Developer > "Set SteamVR as OpenXR runtime" (the menu name may differ in other SteamVR versions). (If a different OpenXR runtime is active the headset
     will not be used and the app falls back to the desktop view.)
  3. Install the current GPU driver. A Windows Defender or SmartScreen prompt may appear for an unsigned exe: choose
     More info > Run anyway (the build is unsigned).

Run
  1. Start SteamVR and wake the headset (the SteamVR status window shows the headset and controllers green).
  2. Double-click $exe. It starts in the headset; the desktop window mirrors the view.
  3. Quit with Alt+F4 on the desktop window.

No headset? It still runs on the desktop: with SteamVR closed or no headset connected the app uses the keyboard-and-mouse
fallback (WASD to move, mouse to look, E / left click to interact). The key list for each scenario is in the project README
in the repository (vive-steamvr\<project>\README.md).

$(if ($isHub) { "THIS HUB BUILD CONTAINS LICENSED ASSET STORE PACKS AND PEOPLE MODELS. Copy it to the lab PC only. Never upload it or publish it.`r`n" } else { "" })General safety-awareness practice, not a procedure or certification.
"@
}

$summary = @()
$builtFolders = @()
if (-not $DryRun) { New-Item -ItemType Directory -Force -Path $LogDir, $OutRoot | Out-Null }

foreach ($name in $all) {
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $path = Join-Path $Vive $name
  $row = [ordered]@{ Project = $name; Status = ''; SizeMB = ''; Seconds = '' }
  Write-Host ''
  Write-Host "=== $name ===" -ForegroundColor Cyan
  try {
    if (-not (Test-Path (Join-Path $path 'ProjectSettings'))) { throw 'not a Unity project folder' }

    if ($name -eq 'hub') {
      $missing = @(Test-HubAssets $path)
      if ($missing.Count) { throw "no build method: the hub's licensed scenes and art are not in this checkout (missing: $($missing -join ', ')). They are git-ignored; see docs\BUILD-THE-HUB.md (copy them from the lab PC with scripts\pack-hub-assets.cmd and unpack-hub-assets.cmd)." }
    }

    $open = @(Get-OpenEditors $path)
    if ($open.Count) { throw "already open in a Unity editor (process id $($open.ProcessId -join ', ')). Close it first." }
    if (Test-Path (Join-Path $path 'Temp\UnityLockfile')) { throw "Temp\UnityLockfile exists: the project is open or Unity did not shut down cleanly. Close Unity; if no editor is open, delete $path\Temp." }

    $target = Get-BuildTarget $path
    if (-not $target) {
      $hint = if ($name -eq 'hub') { ' Expected HubBuild.cs in vive-steamvr\hub\Assets\Editor.' } else { '' }
      throw "no build method found in Assets\Editor (looked for Configurator.BuildWindows).$hint"
    }
    $exeRel = $target.Relative
    $exeName = [IO.Path]::GetFileName($exeRel)
    $builtDir = Join-Path $path ([IO.Path]::GetDirectoryName($exeRel))
    $log = Join-Path $LogDir "$name.log"
    $dest = Join-Path $OutRoot $name

    $unityArgs = @('-accept-apiupdate', '-projectPath', "`"$path`"", '-executeMethod', $target.Method, '-quit', '-logFile', "`"$log`"")
    if ($Batchmode) { $unityArgs = @('-batchmode') + $unityArgs }
    Write-Host "  method : $($target.Method)  ->  $exeRel (inside the project)"
    Write-Host "  command: `"$UnityExe`" $($unityArgs -join ' ')"
    Write-Host "  log    : $log"
    Write-Host "  copy to: $dest"

    if ($DryRun) { $row.Status = 'DRY RUN'; continue }

    if (Test-Path $builtDir) { Remove-Item -Recurse -Force $builtDir }   # no stale output
    $p = Start-Process -FilePath $UnityExe -ArgumentList $unityArgs -PassThru
    $p.WaitForExit()   # not -Wait: that also waits for a UnityCrashHandler64 that can outlive Unity
    Get-Process UnityCrashHandler64 -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    $logText = if (Test-Path $log) { Get-Content -Raw $log } else { '' }
    $errs = @([regex]::Matches($logText, '(?m)^.*error CS\d+.*$') | ForEach-Object { $_.Value.Trim() })
    if ($errs.Count) { throw "compile errors ($($errs.Count)); first: $($errs[0]). See $log" }
    if ($p.ExitCode -ne 0) { throw "Unity exited with code $($p.ExitCode). See $log" }
    if ($logText -notmatch '\[build\] Succeeded') { throw "log has no '[build] Succeeded' line. See $log" }
    $builtExe = Join-Path $path $exeRel
    if (-not (Test-Path $builtExe)) { throw "build reported success but $builtExe is missing" }

    if (Test-Path $dest) { Remove-Item -Recurse -Force $dest }
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Copy-Item -Path (Join-Path $builtDir '*') -Destination $dest -Recurse -Force
    $title = if ($name -eq 'hub') { 'XR Training Hub (lobby and all scenarios)' } else { $name }
    Set-Content -Path (Join-Path $dest 'README-VR-STAGE.txt') -Value (New-StageReadme $title $exeName ($name -eq 'hub')) -Encoding UTF8
    if (-not (Test-Path (Join-Path $dest $exeName))) { throw "copy failed: $exeName not in $dest" }
    if ($name -ne 'hub') { $builtFolders += $dest }   # the hub holds licensed art: its own zip only, never in the combined one
    if (-not $NoZip) {
      $zip = Join-Path $OutRoot "$name.zip"
      if (Test-Path $zip) { Remove-Item -Force $zip }
      Compress-Archive -Path $dest -DestinationPath $zip -CompressionLevel Optimal
    }
    $row.Status = 'OK'
    $row.SizeMB = Get-FolderMB $dest
  }
  catch {
    $msg = $_.Exception.Message
    $row.Status = if ($msg -like 'no build method*') { "SKIPPED: $msg" } else { "FAILED: $msg" }
    Write-Host "  $($row.Status)" -ForegroundColor $(if ($row.Status -like 'SKIPPED*') { 'Yellow' } else { 'Red' })
  }
  finally {
    $row.Seconds = [int]$sw.Elapsed.TotalSeconds
    $summary += [pscustomobject]$row
  }
}

if (-not $DryRun -and -not $NoZip -and $builtFolders.Count) {
  $combined = Join-Path $OutRoot "XR-Training-Vive-$Stamp.zip"
  if (Test-Path $combined) { Remove-Item -Force $combined }
  Compress-Archive -Path $builtFolders -DestinationPath $combined -CompressionLevel Optimal
  Write-Host "`nCombined zip (projects 01 to 04, not the hub): $combined ($([math]::Round((Get-Item $combined).Length / 1MB, 1)) MB)"
}

Write-Host ''
$summary | Format-Table Project, Status, SizeMB, Seconds -AutoSize -Wrap
if ($DryRun) { Write-Host 'Dry run: no Unity process was started and nothing was written.' }
else { Write-Host "Copy the folders in $OutRoot (or the combined zip) to the VR Stage PC. Not tested on that PC or a headset." }
if ($summary | Where-Object { $_.Status -like 'FAILED*' }) { exit 1 }
