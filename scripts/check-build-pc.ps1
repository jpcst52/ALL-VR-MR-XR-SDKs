<#
.SYNOPSIS
  Run this on the PC that will build the Unity players. It checks what the build needs and says what is missing, before you wait an hour
  for a build that cannot work. Changes nothing. Guide: docs\VR-STAGE-QUICKSTART.md

.PARAMETER UnityExe
  Path to Unity.exe (6000.3.20f1). Default: $env:UNITY_EXE, else C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe.
.PARAMETER SkipNetwork
  Do not test the connection to the Unity package server.
#>
[CmdletBinding()]
param([string]$UnityExe, [switch]$SkipNetwork)

$ErrorActionPreference = 'Continue'
$Repo = Split-Path -Parent $PSScriptRoot
$Hub = Join-Path $Repo 'vive-steamvr\hub'
if (-not $UnityExe) { $UnityExe = $env:UNITY_EXE }
if (-not $UnityExe) { $UnityExe = 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe' }

$rows = @()
function Add-Row([string]$name, [string]$status, [string]$detail) { $script:rows += [pscustomobject]@{ Check = $name; Result = $status; Detail = $detail } }

# 1. Unity editor, right version, Windows build support
if (Test-Path $UnityExe) {
  Add-Row 'Unity 6000.3.20f1' 'PASS' $UnityExe
  $support = Join-Path (Split-Path $UnityExe -Parent) 'Data\PlaybackEngines\windowsstandalonesupport'
  if (Test-Path $support) { Add-Row 'Windows Build Support (Mono)' 'PASS' 'module found' }
  else { Add-Row 'Windows Build Support (Mono)' 'FAIL' 'Unity Hub > Installs > 6000.3.20f1 > Add modules > Windows Build Support (Mono)' }
} else {
  Add-Row 'Unity 6000.3.20f1' 'FAIL' "not at $UnityExe. Install it from Unity Hub (Installs > Install Editor > Archive), or pass -UnityExe / set UNITY_EXE"
}

# 2. This checkout
$git = Get-Command git -ErrorAction SilentlyContinue
if ($git) {
  $head = (& git -C $Repo log -1 --format='%h %ad %s' --date=short 2>$null)
  $dirty = @(& git -C $Repo status --short 2>$null | Where-Object { $_ -notmatch '^\?\?' }).Count
  Add-Row 'Repository' 'PASS' "$head$(if ($dirty) { "  ($dirty modified tracked files)" })"
} else { Add-Row 'Repository' 'WARN' 'git not found on PATH (fine if the folder was copied, not cloned)' }
foreach ($p in 'vive-steamvr\packages\com.pittbradford.xrtraining', 'vive-steamvr\01-hydrostatic-test', 'vive-steamvr\hub') {
  if (-not (Test-Path (Join-Path $Repo $p))) { Add-Row "Folder $p" 'FAIL' 'missing: pull the repository again' }
}

# 3. Unity editors open on these projects (a build is refused while a project is open)
$open = @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -and $_.CommandLine -like "*$($Repo.TrimEnd('\'))*" })
if ($open.Count) { Add-Row 'No Unity editor open' 'FAIL' "close it (process id $($open.ProcessId -join ', '))" } else { Add-Row 'No Unity editor open' 'PASS' '' }

# 4. Disk space: five Unity Library folders plus builds
$drive = Get-PSDrive -Name ($Repo.Substring(0, 1)) -ErrorAction SilentlyContinue
if ($drive) {
  $gb = [math]::Round($drive.Free / 1GB, 1)
  $st = if ($gb -ge 25) { 'PASS' } elseif ($gb -ge 15) { 'WARN' } else { 'FAIL' }
  Add-Row 'Free disk space' $st "$gb GB free (needs about 11 GB: Unity import caches for the five projects about 5 GB on the author's PC, hub files 3 GB, builds about 2 GB; plus the Unity editor itself if not installed yet)"
}

# 5. The hub's local-only files
$need = @('Assets\Scenes\Local\Lobby.unity', 'Assets\Scenes\Local\HydrostaticTest_Hub.unity', 'Assets\Scenes\Local\RefineryRounds_Hub.unity',
          'Assets\Scenes\Local\WarehouseForklift_Hub.unity', 'Assets\Scenes\Local\ResistorBench_Hub.unity', 'Assets\Rocketbox', 'Assets\Baked',
          'Assets\IGBlocks', 'Assets\SciFi Warehouse Kit', 'Assets\Warehouse', 'Assets\Modern Office', 'Assets\Shipping Container', 'Assets\Courtyard Props')
$missing = @($need | Where-Object { -not (Test-Path (Join-Path $Hub $_)) })
if (-not $missing.Count) { Add-Row 'Hub licensed files present' 'PASS' 'scenes, Rocketbox and all six art packs found' }
else { Add-Row 'Hub licensed files present' 'WARN' "missing $($missing.Count) of $($need.Count): the hub cannot build until you unpack the hub-assets zip (scripts\unpack-hub-assets.cmd). Projects 01 to 04 do not need it. Missing: $($missing -join ', ')" }

# 5b. Compile errors like "The call is ambiguous between ... TMPro_ExtensionMethods.Compare" or "RuntimeUtilities.BlitFullscreenTriangle" mean the
#     same package code is compiled twice. Look for the usual causes.
$projectsDir = Join-Path $Repo 'vive-steamvr'
$expected = (Get-Content (Join-Path $Hub 'ProjectSettings\ProjectVersion.txt') -ErrorAction SilentlyContinue | Where-Object { $_ -match '^m_EditorVersion:' }) -replace 'm_EditorVersion:\s*', ''
if ($expected -and (Test-Path $UnityExe)) {
  $have = Split-Path (Split-Path (Split-Path $UnityExe -Parent) -Parent) -Leaf
  if ($have -eq $expected) { Add-Row 'Unity version matches the project' 'PASS' $expected }
  else { Add-Row 'Unity version matches the project' 'FAIL' "the hub project was made with $expected but this Unity is $have (project files and package versions differ between Unity versions; install $expected)" }
}
$dupNames = @('TMP_Text.cs', 'TMP_CharacterInfo.cs', 'RuntimeUtilities.cs', 'PostProcessLayer.cs', 'Unity.TextMeshPro.dll', 'Unity.Postprocessing.Runtime.dll', 'Unity.TextMeshPro.asmdef', 'Unity.Postprocessing.Runtime.asmdef')
foreach ($proj in Get-ChildItem $projectsDir -Directory -ErrorAction SilentlyContinue | Where-Object { Test-Path (Join-Path $_.FullName 'Assets') }) {
  $dups = @(Get-ChildItem (Join-Path $proj.FullName 'Assets') -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $dupNames -contains $_.Name })
  if ($dups.Count) { Add-Row "Duplicate TextMesh Pro or Post Processing code in $($proj.Name)" 'FAIL' ("found in Assets (the packages already provide it, so it compiles twice): " + (($dups | Select-Object -First 4 | ForEach-Object { $_.FullName.Substring($projectsDir.Length + 1) }) -join '; ') + ". Delete that folder (or the whole pack folder that holds it) and reopen.") }
  $cache = Join-Path $proj.FullName 'Library\PackageCache'
  if (Test-Path $cache) {
    $twice = @(Get-ChildItem $cache -Directory | Group-Object { ($_.Name -split '@')[0] } | Where-Object { $_.Count -gt 1 })
    if ($twice.Count) { Add-Row "Package cache of $($proj.Name)" 'WARN' ("the same package is cached twice: " + (($twice | ForEach-Object { $_.Name }) -join ', ') + ". Close Unity, delete $($proj.Name)\Library and reopen.") }
  }
}

# 5c. A built player copied into a project's Assets folder is the most common cause of those duplicate-code errors: its *_Data\Managed
#     folder holds Unity.TextMeshPro.dll, Unity.Postprocessing.Runtime.dll and more, which Unity then loads next to the real packages.
foreach ($proj in Get-ChildItem $projectsDir -Directory -ErrorAction SilentlyContinue | Where-Object { Test-Path (Join-Path $_.FullName 'Assets') }) {
  $assetsRoot = Join-Path $proj.FullName 'Assets'
  $player = @(Get-ChildItem $assetsRoot -Recurse -Force -ErrorAction SilentlyContinue | Where-Object { ($_.PSIsContainer -and $_.Name -like '*_Data') -or (-not $_.PSIsContainer -and $_.Name -in @('UnityPlayer.dll', 'UnityCrashHandler64.exe', 'GameAssembly.dll')) })
  if ($player.Count) {
    $where = ($player | ForEach-Object { $_.FullName.Substring($proj.FullName.Length + 1) } | Select-Object -First 4) -join '; '
    Add-Row "Built player inside $($proj.Name)\Assets" 'FAIL' "a copy of a build is in the project, so Unity compiles its DLLs too: $where. Close Unity, delete the folder that contains it (and its .meta file), then reopen."
  }
}
$hubKnown = @('Baked', 'Courtyard Props', 'Editor', 'IGBlocks', 'Modern Office', 'Resources', 'Rocketbox', 'Scenes', 'SciFi Warehouse Kit', 'Scripts', 'Settings', 'Shipping Container', 'Warehouse', 'XR')
$odd = @(Get-ChildItem (Join-Path $Hub 'Assets') -Force -ErrorAction SilentlyContinue | ForEach-Object { $_.Name -replace '\.meta$', '' } | Sort-Object -Unique | Where-Object { $hubKnown -notcontains $_ })
if ($odd.Count) { Add-Row 'Unexpected items in hub\Assets' 'WARN' ("not part of the standard hub: " + ($odd -join ', ') + ". If you did not add them on purpose, close Unity and delete each one together with its .meta file.") }

# 6. Network, only used by Unity to download packages the first time a project opens
if (-not $SkipNetwork) {
  try {
    $ok = (Test-NetConnection -ComputerName 'packages.unity.com' -Port 443 -WarningAction SilentlyContinue -InformationLevel Quiet)
    if ($ok) { Add-Row 'Unity package server reachable' 'PASS' 'packages.unity.com:443' } else { Add-Row 'Unity package server reachable' 'WARN' 'not reachable. The first build needs it to download packages (OpenXR, Input System) unless this PC already has them cached' }
  } catch { Add-Row 'Unity package server reachable' 'WARN' "could not test: $($_.Exception.Message)" }
}

$rows | Format-Table Check, Result, Detail -AutoSize -Wrap
$fail = @($rows | Where-Object Result -eq 'FAIL').Count
$warn = @($rows | Where-Object Result -eq 'WARN').Count
if ($fail) { Write-Host "$fail check(s) FAILED: fix those, then run this again." -ForegroundColor Red; exit 1 }
elseif ($warn) { Write-Host "No failures, $warn warning(s). Projects 01 to 04 can build now; read the warnings before building the hub." -ForegroundColor Yellow }
else { Write-Host 'All checks passed. Next: scripts\build-all-vive.cmd' -ForegroundColor Green }
