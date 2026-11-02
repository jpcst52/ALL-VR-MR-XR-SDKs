@echo off
rem Double-click to start the demo. Add /dashboard to also start the status dashboard.
setlocal
set "EXTRA="
if /i "%~1"=="/dashboard" set "EXTRA=-Dashboard"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-demo.ps1" %EXTRA%
echo.
echo The demo server has stopped. Press any key to close.
pause >nul
