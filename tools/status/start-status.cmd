@echo off
cd /d "%~dp0\..\.."
start "" /b node tools\status\open-when-ready.cjs
node tools\status\server.cjs
