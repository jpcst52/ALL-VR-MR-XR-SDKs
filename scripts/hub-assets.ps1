<#
.SYNOPSIS
  Packs or unpacks the hub project's local-only files (licensed Asset Store packs, Rocketbox people, the hub scenes and baked data)
  so the hub can be built on another PC. Full steps: docs\BUILD-THE-HUB.md

.DESCRIPTION
  Git commits only the hub's own code (Assets\Editor, Assets\Scripts, Assets\Settings, Assets\XR, see vive-steamvr\hub\.gitignore).
  Everything else under vive-steamvr\hub\Assets is local. -Pack zips all of it, with the .meta files (they hold the asset GUIDs the scenes
  refer to, so copy them too), into one zip. -Unpack puts such a zip into another checkout.

  THE ZIP HOLDS LICENSED ASSET STORE FILES. Move it from PC to PC (USB drive, local network) only. Never upload it to GitHub, a cloud
  drive that others can open, or email.

.PARAMETER Pack
  Create the zip from this checkout. Default output: builds\hub-assets-<date>.zip (the builds folder is git-ignored).
.PARAMETER Unpack
  Path of a zip made by -Pack; extracts it into vive-steamvr\hub\Assets of this checkout (overwrites files with the same name).
.PARAMETER HubPath
  The hub project folder. Default: vive-steamvr\hub of this repository.
.PARAMETER Out
  Output zip for -Pack.
.PARAMETER DryRun
  With -Pack: list what would be packed and its size; write nothing.
#>
[CmdletBinding()]
param(
  [switch]$Pack,
  [string]$Unpack,
  [string]$HubPath,
  [string]$Out,
  [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$Repo = Split-Path -Parent $PSScriptRoot
if (-not $HubPath) { $HubPath = Join-Path $Repo 'vive-steamvr\hub' }
$Assets = Join-Path $HubPath 'Assets'
# Top-level entries of Assets that git tracks (our own code); everything else is local. Keep in step with vive-steamvr\hub\.gitignore.
$tracked = @('Editor', 'Scripts', 'Settings', 'XR')

if ($Pack -eq ($Unpack -ne '' -and $null -ne $Unpack)) { throw 'Use exactly one of -Pack or -Unpack <zip>.' }
if (-not (Test-Path $Assets)) { throw "No Assets folder at $Assets" }
$Assets = [IO.Path]::GetFullPath($Assets)

if ($Pack) {
  $items = Get-ChildItem $Assets -Force | Where-Object { $tracked -notcontains ($_.Name -replace '\.meta$', '') }
  $files = foreach ($i in $items) { if ($i.PSIsContainer) { Get-ChildItem $i.FullName -Recurse -File -Force } else { $i } }
  $files = @($files)
  if (-not $files.Count) { throw "Nothing to pack under $Assets (this looks like a fresh clone: the hub's local files are not here)." }
  $mb = [math]::Round(($files | Measure-Object Length -Sum).Sum / 1MB)
  Write-Host "Local-only hub files: $($files.Count) files, $mb MB, from $Assets"
  $items | Where-Object PSIsContainer | ForEach-Object { Write-Host ("  {0}" -f $_.Name) }
  $must = @('Scenes\Local\Lobby.unity', 'Rocketbox', 'IGBlocks')
  foreach ($m in $must) { if (-not (Test-Path (Join-Path $Assets $m))) { Write-Warning "Expected $m is missing; the hub will not build without it." } }
  if ($DryRun) { Write-Host 'Dry run: nothing written.'; return }
  if (-not $Out) { $Out = Join-Path $Repo ("builds\hub-assets-{0}.zip" -f (Get-Date -Format 'yyyy-MM-dd')) }
  New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Out) | Out-Null
  if (Test-Path $Out) { Remove-Item -Force $Out }
  $root = $Assets.TrimEnd('\') + '\'   # same spelling as the enumerated files, so the relative paths are exact
  $zip = [IO.Compression.ZipFile]::Open($Out, 'Create')
  try {
    $n = 0
    foreach ($f in $files) {
      $rel = $f.FullName.Substring($root.Length).Replace('\', '/')
      [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $f.FullName, $rel, [IO.Compression.CompressionLevel]::Fastest)
      $n++
      if ($n % 2000 -eq 0) { Write-Host "  $n / $($files.Count) files" }
    }
  } finally { $zip.Dispose() }
  Write-Host ("Wrote {0} ({1} MB). Licensed files inside: move it by USB or local network only, never upload it." -f $Out, [math]::Round((Get-Item $Out).Length / 1MB))
  return
}

# Unpack
if (-not (Test-Path $Unpack)) { throw "Zip not found: $Unpack" }
if (Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -and $_.CommandLine -like "*$($HubPath.TrimEnd('\'))*" }) {
  throw 'The hub is open in a Unity editor. Close it first.'
}
Write-Host "Extracting $Unpack into $Assets"
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $Unpack).Path)
try {
  $n = 0
  foreach ($e in $zip.Entries) {
    if (-not $e.Name) { continue }                                  # a folder entry
    $dest = Join-Path $Assets ($e.FullName -replace '/', '\')
    if ($dest -notlike ($Assets.TrimEnd('\') + '\*')) { throw "Unsafe path in zip: $($e.FullName)" }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $dest, $true)
    $n++
    if ($n % 2000 -eq 0) { Write-Host "  $n files" }
  }
} finally { $zip.Dispose() }
Write-Host "Done: $n files. Next: scripts\build-all-vive.cmd (or open the hub in Unity once first, see docs\BUILD-THE-HUB.md)."
