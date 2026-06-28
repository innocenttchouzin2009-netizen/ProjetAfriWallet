@echo off
setlocal
set "PATH=C:\Program Files\nodejs;%PATH%"
cd /d "%~dp0"
if exist node_modules rd /s /q node_modules
"C:\Program Files\nodejs\npm.cmd" install
endlocal
