<#
.SYNOPSIS
  Turns SteamVR's simulated headset (the "null" driver) on or off, so the Vive/SteamVR builds can be started and looked at with no headset
  connected. Guide: docs\STEAMVR-SIMULATOR.md

.DESCRIPTION
  The simulated headset has no controllers and no tracking: it gives the app a headset that is "connected", renders to a desktop window, and
  sits still. It is good for: does the build start, does SteamVR accept it, does it render in stereo, what does it look like at standing height.
  It cannot test controllers, grabbing, teleporting, comfort or frame rate on real hardware.

  -Enable   backs up steamvr.vrsettings (and chaperone_info.vrchap if there is one), switches the null driver on, writes a chaperone file that puts the
            floor <Height> metres below the headset (otherwise the camera is on the floor), restarts SteamVR and closes the room-setup wizard.
  -Disable  puts the backed-up files back (or removes the ones this script made) and stops SteamVR.
  -Status   shows what is set now.
  -Run <exe> [-Shot <png>] [-Seconds 30]   starts a build, waits, saves a picture of its window, and stops it (SteamVR must be running).

  This edits SteamVR's settings for the whole PC. Do not leave it enabled on a PC that is also used with a real headset.

.PARAMETER Height
  Headset height above the floor in metres for the simulated headset. Default 1.6.
#>
[CmdletBinding()]
param(
  [switch]$Enable, [switch]$Disable, [switch]$Status,
  [string]$Run, [string]$Shot, [int]$Seconds = 30,
  [double]$Height = 1.6, [string]$SteamPath
)

$ErrorActionPreference = 'Stop'
if (-not $SteamPath) {
  $reg = Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue
  $SteamPath = if ($reg -and $reg.SteamPath) { $reg.SteamPath -replace '/', '\' } else { 'C:\Program Files (x86)\Steam' }
}
$cfg = Join-Path $SteamPath 'config'
$settings = Join-Path $cfg 'steamvr.vrsettings'
$chap = Join-Path $cfg 'chaperone_info.vrchap'
$bakSettings = "$settings.bak-before-simulator"
$bakChap = "$chap.bak-before-simulator"
$madeChap = "$chap.made-by-simulator-script"
$vrBin = Join-Path $SteamPath 'steamapps\common\SteamVR\bin\win64'
if (-not (Test-Path (Join-Path $vrBin 'vrstartup.exe'))) { throw "SteamVR is not installed under $SteamPath (Steam > Library > Tools > SteamVR)." }

function Stop-SteamVR {
  foreach ($p in 'vrmonitor', 'vrserver', 'vrcompositor', 'vrdashboard', 'vrwebhelper', 'vrstartup', 'vrserverhelper', 'steamvr_room_setup') {
    Get-Process $p -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
  }
}
function Start-SteamVR {
  Start-Process -FilePath (Join-Path $vrBin 'vrstartup.exe') -WorkingDirectory $vrBin | Out-Null
  $until = (Get-Date).AddSeconds(40)
  while ((Get-Date) -lt $until -and -not (Get-Process vrserver -ErrorAction SilentlyContinue)) { Start-Sleep -Seconds 1 }
  Start-Sleep -Seconds 12
  Get-Process steamvr_room_setup -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue   # it waits for real controllers and cannot finish
}
function Read-Json($p) { if (Test-Path $p) { Get-Content -Raw $p | ConvertFrom-Json } else { [pscustomobject]@{} } }
function Write-Json($o, $p) { [IO.File]::WriteAllText($p, ($o | ConvertTo-Json -Depth 20), (New-Object Text.UTF8Encoding($false))) }

if ($Status) {
  $s = Read-Json $settings
  $on = $s.driver_null -and $s.driver_null.enable
  Write-Host "Simulated headset (null driver): $(if ($on) { 'ON' } else { 'off' })"
  Write-Host "Chaperone file made by this script: $(Test-Path $madeChap)"
  Write-Host "SteamVR running: $([bool](Get-Process vrserver -ErrorAction SilentlyContinue))"
  return
}

if ($Enable) {
  if ((Test-Path $settings) -and -not (Test-Path $bakSettings)) { Copy-Item $settings $bakSettings }
  if ((Test-Path $chap) -and -not (Test-Path $bakChap) -and -not (Test-Path $madeChap)) { Copy-Item $chap $bakChap }
  $s = Read-Json $settings
  if (-not $s.steamvr) { $s | Add-Member -NotePropertyName steamvr -NotePropertyValue ([pscustomobject]@{}) }
  $s.steamvr | Add-Member -NotePropertyName requireHmd -NotePropertyValue $false -Force
  $s.steamvr | Add-Member -NotePropertyName forcedDriver -NotePropertyValue 'null' -Force
  $s.steamvr | Add-Member -NotePropertyName activateMultipleDrivers -NotePropertyValue $true -Force
  $null = $s | Add-Member -NotePropertyName driver_null -NotePropertyValue ([pscustomobject]@{ enable = $true; windowX = 50; windowY = 50; windowWidth = 1080; windowHeight = 600; renderWidth = 1512; renderHeight = 1680; displayFrequency = 90.0 }) -Force
  Stop-SteamVR
  Write-Json $s $settings
  # The null headset sits at the tracking origin, i.e. on the floor. SteamVR finds the floor in the chaperone file (universe 2 is the null driver's).
  # A POSITIVE y here lifts the headset to that height above the floor; a negative one puts the camera under the floor (everything lit looks black).
  $b = @(@(@(-1, 0, -1), @(-1, 2.4, -1), @(-1, 2.4, 1), @(-1, 0, 1)), @(@(-1, 0, 1), @(-1, 2.4, 1), @(1, 2.4, 1), @(1, 0, 1)),
         @(@(1, 0, 1), @(1, 2.4, 1), @(1, 2.4, -1), @(1, 0, -1)), @(@(1, 0, -1), @(1, 2.4, -1), @(-1, 2.4, -1), @(-1, 0, -1)))
  $uni = foreach ($id in '1', '2', '3') {
    [ordered]@{ collision_bounds = $b; play_area = @(2.0, 2.0); seated = [ordered]@{ translation = @(0.0, 0.0, 0.0); yaw = 0.0 }
                standing = [ordered]@{ translation = @(0.0, $Height, 0.0); yaw = 0.0 }; time = (Get-Date).ToString('ddd MMM d HH:mm:ss yyyy'); universeID = $id }
  }
  Write-Json ([ordered]@{ jsonid = 'chaperone_info'; universes = @($uni); version = 5 }) $chap
  Set-Content -Path $madeChap -Value 'created by scripts\steamvr-simulator.ps1' -Encoding ASCII
  Start-SteamVR
  Write-Host "Simulated headset ON (headset $Height m above the floor). Start a build now. Undo with: scripts\steamvr-simulator.ps1 -Disable" -ForegroundColor Green
  return
}

if ($Disable) {
  Stop-SteamVR
  if (Test-Path $bakSettings) { Copy-Item $bakSettings $settings -Force; Remove-Item $bakSettings }
  if (Test-Path $bakChap) { Copy-Item $bakChap $chap -Force; Remove-Item $bakChap }
  elseif (Test-Path $madeChap) { Remove-Item $chap -ErrorAction SilentlyContinue }
  Remove-Item $madeChap -ErrorAction SilentlyContinue
  Write-Host 'Simulated headset OFF; SteamVR settings restored. SteamVR is stopped; start it from Steam when you need it.' -ForegroundColor Green
  return
}

if ($Run) {
  if (-not (Test-Path $Run)) { throw "Not found: $Run" }
  if (-not (Get-Process vrserver -ErrorAction SilentlyContinue)) { throw 'SteamVR is not running. Run -Enable first.' }
  $log = Join-Path $env:TEMP ("sim-run-" + [IO.Path]::GetFileNameWithoutExtension($Run) + '.log')
  $p = Start-Process -FilePath $Run -ArgumentList @('-screen-width', '960', '-screen-height', '540', '-pbHeadOffset', '1.6', '-logFile', "`"$log`"") -PassThru
  Start-Sleep -Seconds $Seconds
  if ($Shot) {
    Add-Type -AssemblyName System.Drawing
    Add-Type -TypeDefinition @'
using System; using System.Runtime.InteropServices;
public class SimWin { [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f); [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r); public struct RECT { public int L, T, R, B; } }
'@
    $p.Refresh()
    if ($p.MainWindowHandle -ne [IntPtr]::Zero) {
      $r = New-Object SimWin+RECT; [void][SimWin]::GetWindowRect($p.MainWindowHandle, [ref]$r)
      $bmp = New-Object System.Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
      $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc(); [void][SimWin]::PrintWindow($p.MainWindowHandle, $hdc, 2); $g.ReleaseHdc($hdc)
      New-Item -ItemType Directory -Force (Split-Path -Parent $Shot) | Out-Null
      $bmp.Save($Shot); Write-Host "Saved $Shot ($($bmp.Width) x $($bmp.Height))"
    } else { Write-Warning 'The build has no window to capture (it may have exited). See the log.' }
  }
  $alive = -not $p.HasExited
  if ($alive) { Stop-Process -Id $p.Id -Force }
  $t = if (Test-Path $log) { Get-Content -Raw $log } else { '' }
  $exc = ([regex]::Matches($t, 'Exception')).Count
  Write-Host ("Ran {0} s, still running at the end: {1}, exceptions in the log: {2}. Log: {3}" -f $Seconds, $alive, $exc, $log)
  if ($t -match 'Initialized\. Connected to ([^\r\n]+)') { Write-Host "SteamVR: connected to $($Matches[1])" }
  return
}

Write-Host 'Use -Enable, -Disable, -Status, or -Run <exe> [-Shot <png>]. See the comment at the top of this file.'
