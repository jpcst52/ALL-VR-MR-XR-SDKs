@echo off
rem Double-click: builds Valve's SteamVR plugin sample (Interaction System) so you can check the Vive with it. Close every Unity editor first.
rem Guide: docs\STEAMVR-SAMPLE.md
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup-steamvr-sample.ps1" %*
echo.
if exist "%~dp0..\builds\vive\steamvr-sample" start "" explorer "%~dp0..\builds\vive\steamvr-sample"
pause
