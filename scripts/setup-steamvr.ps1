<#
.SYNOPSIS
  Moves the Vive / SteamVR training projects onto Valve's SteamVR plugin and its Interaction System (Player, hands, Teleporting with TeleportArea,
  Throwable). Guide: docs\STEAMVR-REBUILD.md

.DESCRIPTION
  For each project: download the plugin once (Valve, BSD-3-Clause, release 2.8.5b) -> import -> fix four lines Unity 6.3 rejects
  (PhysicMaterial is now PhysicsMaterial) -> generate Steam Input actions from Valve's example files -> OpenVR loader, Direct3D 11,
  the PB_STEAMVR define that switches the kit's SteamVR backend on, and the Player and Teleporting prefabs the backend loads.
  The plugin files (about 100 MB per project) are git-ignored; this script puts them back. Run it again any time; finished steps are skipped.
  Needs Unity 6000.3.20f1 and every Unity editor closed.

.PARAMETER Projects
  Project folder names under vive-steamvr. Default: the four scenario projects. The hub is included with -IncludeHub (lab PC only).
.PARAMETER PluginPackage
  A steamvr_*.unitypackage you already downloaded (skips the download).
#>
[CmdletBinding()]
param([string[]]$Projects, [switch]$IncludeHub, [string]$UnityExe, [string]$PluginPackage)

$ErrorActionPreference = 'Stop'
$Tag = '2.8.5b'
$Asset = 'steamvr_2_8_5.unitypackage'
$Repo = Split-Path -Parent $PSScriptRoot
$Vive = Join-Path $Repo 'vive-steamvr'
if (-not $UnityExe) { $UnityExe = $env:UNITY_EXE }
if (-not $UnityExe) { $UnityExe = 'D:\Program Files\Unity\Editor\6000.3.20f1\Editor\Unity.exe' }
if (-not (Test-Path $UnityExe)) { throw "Unity.exe not found: $UnityExe" }
if (-not $Projects) { $Projects = @('01-hydrostatic-test', '02-refinery-rounds', '03-warehouse-forklift', '04-resistor-bench') }
if ($IncludeHub) { $Projects += 'hub' }
$LogDir = Join-Path $Repo 'builds\logs'
New-Item -ItemType Directory -Force $LogDir | Out-Null
$Setup = Join-Path $Vive 'steamvr-setup\SteamVRProjectSetup.cs'

function Invoke-UnityStep([string]$project, [string]$name, [string[]]$more, [switch]$AllowCompileErrors) {
  $path = Join-Path $Vive $project
  $log = Join-Path $LogDir "steamvr-$project-$name.log"
  $a = @('-batchmode', '-nographics', '-accept-apiupdate', '-projectPath', "`"$path`"") + $more + @('-logFile', "`"$log`"")
  Write-Host "  [$name] Unity ..." -ForegroundColor Cyan
  $p = Start-Process -FilePath $UnityExe -ArgumentList $a -PassThru
  $p.WaitForExit()   # not -Wait: that also waits for a UnityCrashHandler64 that can outlive Unity
  Get-Process UnityCrashHandler64 -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
  $text = ''
  if (Test-Path $log) { $text = Get-Content -Raw $log }
  $errs = @([regex]::Matches($text, '(?m)^.*error CS\d+.*$') | ForEach-Object { $_.Value.Trim() })
  if ($errs.Count -and -not $AllowCompileErrors) { throw "compile errors in '$project' step '$name' ($($errs.Count)); first: $($errs[0]). Log: $log" }
  if ($p.ExitCode -ne 0 -and -not $AllowCompileErrors) { throw "Unity exited with code $($p.ExitCode) in '$project' step '$name'. Log: $log" }
  return $text
}

$pkg = $PluginPackage
if (-not $pkg) {
  $pkg = Join-Path $env:TEMP $Asset
  if (-not (Test-Path $pkg) -or (Get-Item $pkg).Length -lt 50MB) {
    Write-Host "Downloading the SteamVR plugin $Tag from github.com/ValveSoftware/steamvr_unity_plugin ..."
    Invoke-WebRequest -Uri "https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/$Tag/$Asset" -OutFile $pkg -UseBasicParsing
  }
}

foreach ($project in $Projects) {
  $path = Join-Path $Vive $project
  Write-Host "=== $project" -ForegroundColor Cyan
  if (-not (Test-Path (Join-Path $path 'ProjectSettings'))) { throw "Not a Unity project: $path" }
  $open = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -and $_.CommandLine -like "*$($path.TrimEnd('\'))*" }
  if ($open) { throw "$project is open in a Unity editor. Close it first." }
  if ($project -eq 'hub') { foreach ($need in 'Assets\Scenes\Local\Lobby.unity', 'Assets\Rocketbox') { if (-not (Test-Path (Join-Path $path $need))) { throw "The hub's licensed files are missing ($need). See docs\BUILD-THE-HUB.md" } } }

  # the plugin needs these packages and Unity modules; the project's other packages are left alone
  $manifest = Join-Path $path 'Packages\manifest.json'
  $json = Get-Content -Raw $manifest | ConvertFrom-Json
  $changed = $false
  $needed = @(@('com.unity.ugui', '2.0.0'), @('com.unity.xr.management', '4.5.4'))
  foreach ($m in 'ai', 'animation', 'audio', 'director', 'imageconversion', 'imgui', 'jsonserialize', 'particlesystem', 'physics', 'screencapture', 'terrain', 'ui', 'uielements', 'unitywebrequest', 'vehicles', 'vr', 'xr') { $needed += , @("com.unity.modules.$m", '1.0.0') }
  foreach ($pair in $needed) {
    if (-not $json.dependencies.PSObject.Properties[$pair[0]]) { $json.dependencies | Add-Member -NotePropertyName $pair[0] -NotePropertyValue $pair[1]; $changed = $true }
  }
  if ($changed) { [IO.File]::WriteAllText($manifest, ($json | ConvertTo-Json -Depth 10), (New-Object Text.UTF8Encoding($false))) }

  if (-not (Test-Path (Join-Path $path 'Assets\SteamVR'))) {
    # the import start itself stops on the four known compile errors (fixed next); the files are copied in by then
    [void](Invoke-UnityStep $project 'import' @('-importPackage', "`"$pkg`"", '-quit') -AllowCompileErrors)
  }
  if (-not (Test-Path (Join-Path $path 'Assets\SteamVR'))) { throw "The plugin files were not imported into $project. See builds\logs\steamvr-$project-import.log" }

  foreach ($f in @('Assets\SteamVR\InteractionSystem\Core\Scripts\HandCollider.cs', 'Assets\SteamVR\InteractionSystem\Longbow\Scripts\Arrow.cs')) {
    $file = Join-Path $path $f
    $t = [IO.File]::ReadAllText($file)
    $n = $t -replace '\bPhysicMaterialCombine\b', 'PhysicsMaterialCombine' -replace '\bPhysicMaterial\b', 'PhysicsMaterial'
    if ($n -ne $t) { [IO.File]::WriteAllText($file, $n, (New-Object Text.UTF8Encoding($true))); Write-Host "  patched $f" }
  }

  New-Item -ItemType Directory -Force (Join-Path $path 'Assets\Editor') | Out-Null
  Copy-Item $Setup (Join-Path $path 'Assets\Editor\SteamVRProjectSetup.cs') -Force
  [void](Invoke-UnityStep $project 'compile' @('-quit'))
  [void](Invoke-UnityStep $project 'input' @('-executeMethod', 'SteamVRProjectSetup.Step1Input'))
  [void](Invoke-UnityStep $project 'project' @('-executeMethod', 'SteamVRProjectSetup.Step2Project'))
  [void](Invoke-UnityStep $project 'final-compile' @('-quit'))
  Write-Host "  $project is on the SteamVR plugin. Next: scripts\build-vive.cmd" -ForegroundColor Green
}
