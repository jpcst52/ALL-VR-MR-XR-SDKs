@echo off
rem Double-click: builds all five SteamVR / Vive players (projects 01 to 04 and the hub) into builds\vive\, one folder and one zip each,
rem plus XR-Training-Vive-<date>.zip with projects 01 to 04. Then opens the folder so you can copy it to the VR Stage PC.
rem Needs Unity 6000.3.20f1 and every Unity editor CLOSED. Takes about 45 minutes (the hub alone is about 11). Details: docs\BUILDS.md
rem The hub needs its licensed scenes and art, which are not in git; without them it is reported as SKIPPED and the other four still build.
setlocal
echo Building all five Unity players. Close every Unity editor first. This takes about 45 minutes.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-vive.ps1" -IncludeHub %*
if exist "%~dp0..\builds\vive" start "" explorer "%~dp0..\builds\vive"
echo.
pause
