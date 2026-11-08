# Compiles a Unity project without opening a window and lists any C# errors. Run it after editing anything in the shared kit
# (vive-steamvr/packages/com.pittbradford.xrtraining): one bad file there puts every project into the Safe Mode prompt.
#   powershell -File scripts\unity-compile-check.ps1 [-Project 02-refinery-rounds]
# Use a project that is NOT open in the Unity editor (Unity allows one editor per project).
param(
    [string]$Project = "02-refinery-rounds",
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe"
)
$root = Split-Path -Parent $PSScriptRoot
$path = Join-Path $root "vive-steamvr\$Project"
$log = Join-Path $env:TEMP "unity-compile-check-$Project.log"
if (Test-Path $log) { Remove-Item $log }
Write-Host "Compiling $Project (no window) ..."
$p = Start-Process -FilePath $Unity -ArgumentList @("-batchmode", "-nographics", "-quit", "-projectPath", $path, "-logFile", $log) -PassThru -WindowStyle Hidden
if (-not $p.WaitForExit(600000)) { $p.Kill(); Write-Host "Timed out after 10 minutes"; exit 2 }
$errors = Select-String -Path $log -Pattern "error CS\d+" -ErrorAction SilentlyContinue | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
if ($errors) {
    Write-Host "COMPILE ERRORS ($($errors.Count)):"
    $errors | ForEach-Object { Write-Host "  $_" }
    exit 1
}
Write-Host "No C# compile errors (Unity exit code $($p.ExitCode)). Log: $log"
exit 0
