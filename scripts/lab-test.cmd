@echo off
rem Double-click for lab testing: opens the folder with the Unity Vive builds, then starts the demo server
rem (HTTPS relay + all web pages) and opens the launcher. Close this window to stop the server.
rem Steps for the day: docs\LAB-TEST-TODAY.md
setlocal
set "REPO=%~dp0.."
if exist "%REPO%\builds\vive" (
  start "" explorer "%REPO%\builds\vive"
) else (
  echo No Unity builds yet. Run scripts\build-vive.cmd to make them. Starting the server anyway.
)
call "%~dp0start-demo.cmd"
