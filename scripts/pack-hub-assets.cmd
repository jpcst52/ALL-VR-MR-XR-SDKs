@echo off
rem Run on the PC that has the hub's licensed art: zips the hub's local-only files (Asset Store packs, Rocketbox, scenes, baked data)
rem into builds\hub-assets-<date>.zip (about 3 GB of files) so another PC can build the hub. Steps: docs\BUILD-THE-HUB.md
rem THE ZIP HOLDS LICENSED FILES: move it by USB or local network only, never upload it.
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0hub-assets.ps1" -Pack %*
echo.
if exist "%~dp0..\builds" start "" explorer "%~dp0..\builds"
pause
