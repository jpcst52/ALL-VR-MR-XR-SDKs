@echo off
rem On the PC that will build the hub: drag the hub-assets zip onto this file (or run: unpack-hub-assets.cmd path\to\hub-assets.zip).
rem Extracts into vive-steamvr\hub\Assets. Close any Unity editor first. Steps: docs\BUILD-THE-HUB.md
setlocal
if "%~1"=="" (
  echo Drag the hub-assets zip onto this file, or run:  unpack-hub-assets.cmd path\to\hub-assets-DATE.zip
  pause
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0hub-assets.ps1" -Unpack "%~1"
echo.
pause
