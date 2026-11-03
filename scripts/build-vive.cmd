@echo off
rem Double-click to build the Vive / SteamVR players into builds\vive\. Extra arguments pass through, for example:
rem   build-vive.cmd -IncludeHub        build the hub too (lab PC only)
rem   build-vive.cmd -DryRun            print the commands, build nothing
rem Close every Unity editor that has one of the projects open first.
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-vive.ps1" %*
echo.
pause
