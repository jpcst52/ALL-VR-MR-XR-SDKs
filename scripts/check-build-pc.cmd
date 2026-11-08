@echo off
rem Run on the PC that will build the Unity players: checks Unity, disk space, the hub's licensed files and the network. Changes nothing.
rem Guide: docs\VR-STAGE-QUICKSTART.md
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-build-pc.ps1" %*
echo.
pause
