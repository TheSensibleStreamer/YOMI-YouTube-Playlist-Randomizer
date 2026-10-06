@echo off
setlocal
cd /d "%~dp0"
if exist "%ProgramFiles%\YOMI\VERSION.txt" (
    powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0installer\install.ps1" -UpdateMode
) else (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0installer\install.ps1"
)
exit /b %ERRORLEVEL%
