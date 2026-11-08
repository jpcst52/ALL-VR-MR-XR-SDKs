<#
.SYNOPSIS
  Makes a Windows build of Valve's own SteamVR plugin sample (Interaction System, "Interactions_Example") with Unity 6000.3, so you can check on
  the Vive that Valve's plugin works on this PC before anything of ours is built on it. Guide: docs\STEAMVR-SAMPLE.md

.DESCRIPTION
  Steps: download the plugin (Valve, BSD-3-Clause, release 2.8.5b) -> new Unity project -> import -> fix four lines Unity 6.3 rejects
  (PhysicMaterial is now PhysicsMaterial) -> generate Steam Input actions -> OpenVR loader, Direct3D 11 -> build.
  Output: builds\vive\steamvr-sample\SteamVRSample.exe. The project (about 100 MB of plugin files) is created in vive-steamvr\steamvr-sample and is git-ignored.
  Needs: Unity 6000.3.20f1 with Windows Build Support (Mono), internet for the first download, every Unity editor closed.

.PARAMETER Project
  Project folder to create. Default: vive-steamvr\steamvr-sample.
.PARAMETER UnityExe
  Unity.exe. Default: $env:UNITY_EXE, else C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe.
.PARAMETER PluginPackage
  A steamvr_*.unitypackage you already downloaded (skips the download).
#>
[CmdletBinding()]
param([string]$Project, [string]$UnityExe, [string]$PluginPackage)

$ErrorActionPreference = 'Stop'
$Tag = '2.8.5b'                       # first release with the Unity 6 Direct3D 12 fix (earlier ones show no picture in Unity 6)
$Asset = 'steamvr_2_8_5.unitypackage'
$Repo = Split-Path -Parent $PSScriptRoot
if (-not $Project) { $Project = Join-Path $Repo 'vive-steamvr\steamvr-sample' }
if (-not $UnityExe) { $UnityExe = $env:UNITY_EXE }
if (-not $UnityExe) { $UnityExe = 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe' }
if (-not (Test-Path $UnityExe)) { throw "Unity.exe not found: $UnityExe. Install Unity 6000.3.20f1 or set UNITY_EXE." }
$open = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -and $_.CommandLine -like "*$($Project.TrimEnd('\'))*" }
if ($open) { throw 'That project is open in a Unity editor. Close it first.' }
$LogDir = Join-Path $Repo 'builds\logs'
New-Item -ItemType Directory -Force $LogDir | Out-Null
$Setup = Join-Path $Repo 'vive-steamvr\steamvr-setup\SteamVRSampleSetup.cs'

function Run-Unity([string]$name, [string[]]$more, [switch]$NoProject, [switch]$AllowCompileErrors) {
  $log = Join-Path $LogDir "steamvr-sample-$name.log"
  $a = @('-batchmode', '-nographics')
  if (-not $NoProject) { $a += @('-projectPath', "`"$Project`"") }
  $a += $more
  $a += @('-logFile', "`"$log`"")
  Write-Host "  [$name] Unity ..." -ForegroundColor Cyan
  $p = Start-Process -FilePath $UnityExe -ArgumentList $a -Wait -PassThru
  $text = ''
  if (Test-Path $log) { $text = Get-Content -Raw $log }
  $errs = @([regex]::Matches($text, '(?m)^.*error CS\d+.*$') | ForEach-Object { $_.Value.Trim() })
  if ($errs.Count -and -not $AllowCompileErrors) { throw "compile errors in step '$name' ($($errs.Count)); first: $($errs[0]). Log: $log" }
  if ($p.ExitCode -ne 0 -and -not $AllowCompileErrors) { throw "Unity exited with code $($p.ExitCode) in step '$name'. Log: $log" }
  return $text
}

Write-Host "SteamVR plugin sample -> $Project"
$pkg = $PluginPackage
if (-not $pkg) {
  $pkg = Join-Path $env:TEMP $Asset
  if (-not (Test-Path $pkg) -or (Get-Item $pkg).Length -lt 50MB) {
    Write-Host "  downloading the SteamVR plugin $Tag from github.com/ValveSoftware/steamvr_unity_plugin ..."
    Invoke-WebRequest -Uri "https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/$Tag/$Asset" -OutFile $pkg -UseBasicParsing
  }
}
if (-not (Test-Path (Join-Path $Project 'ProjectSettings'))) {
  if (Test-Path $Project) { Remove-Item -Recurse -Force $Project }
  [void](Run-Unity 'create' @('-createProject', "`"$Project`"", '-quit') -NoProject)
}
# packages the plugin's scripts need
$manifest = Join-Path $Project 'Packages\manifest.json'
$json = Get-Content -Raw $manifest | ConvertFrom-Json
$json.dependencies | Add-Member -NotePropertyName 'com.unity.ugui' -NotePropertyValue '2.0.0' -Force
$json.dependencies | Add-Member -NotePropertyName 'com.unity.xr.management' -NotePropertyValue '4.5.4' -Force
[IO.File]::WriteAllText($manifest, ($json | ConvertTo-Json -Depth 10), (New-Object Text.UTF8Encoding($false)))   # no byte-order mark: Unity rejects one

if (-not (Test-Path (Join-Path $Project 'Assets\SteamVR'))) {
  # the import start itself stops on the four known compile errors (fixed next); the files are copied in by then
  [void](Run-Unity 'import' @('-importPackage', "`"$pkg`"", '-quit') -AllowCompileErrors)
}
if (-not (Test-Path (Join-Path $Project 'Assets\SteamVR'))) { throw 'The plugin files were not imported. See builds\logs\steamvr-sample-import.log' }

# Unity 6.3 turned PhysicMaterial into an error (it is PhysicsMaterial now); two files in the plugin use it
$files = @('Assets\SteamVR\InteractionSystem\Core\Scripts\HandCollider.cs', 'Assets\SteamVR\InteractionSystem\Longbow\Scripts\Arrow.cs')
foreach ($f in $files) {
  $path = Join-Path $Project $f
  $t = [IO.File]::ReadAllText($path)
  $n = $t -replace '\bPhysicMaterialCombine\b', 'PhysicsMaterialCombine' -replace '\bPhysicMaterial\b', 'PhysicsMaterial'
  if ($n -ne $t) { [IO.File]::WriteAllText($path, $n, (New-Object Text.UTF8Encoding($true))); Write-Host "  patched $f" }
}

New-Item -ItemType Directory -Force (Join-Path $Project 'Assets\Editor') | Out-Null
Copy-Item $Setup (Join-Path $Project 'Assets\Editor\SteamVRSampleSetup.cs') -Force
[void](Run-Unity 'compile' @('-quit'))
[void](Run-Unity 'input' @('-executeMethod', 'SteamVRSampleSetup.Step1Input'))
[void](Run-Unity 'project' @('-executeMethod', 'SteamVRSampleSetup.Step2Project'))
$text = Run-Unity 'build' @('-executeMethod', 'SteamVRSampleSetup.BuildWindows', '-quit')
if ($text -notmatch '\[build\] Succeeded') { throw 'The build did not report success. See builds\logs\steamvr-sample-build.log' }

$built = Join-Path $Project 'Builds\SteamVRSample'
$dest = Join-Path $Repo 'builds\vive\steamvr-sample'
if (Test-Path $dest) { Remove-Item -Recurse -Force $dest }
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item (Join-Path $built '*') $dest -Recurse -Force
Write-Host ''
Write-Host "Done: $dest\SteamVRSample.exe" -ForegroundColor Green
Write-Host 'Copy the whole folder to the VR Stage PC, start SteamVR, run SteamVRSample.exe. Not tested on a headset yet.'
