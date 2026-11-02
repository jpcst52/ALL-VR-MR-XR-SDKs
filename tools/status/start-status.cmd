@echo off
cd /d "%~dp0\..\.."
start "" http://localhost:5210
node tools\status\server.cjs
