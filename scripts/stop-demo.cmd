@echo off
rem Double-click to stop the demo server (only this demo, nothing else).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0stop-demo.ps1"
echo.
pause
