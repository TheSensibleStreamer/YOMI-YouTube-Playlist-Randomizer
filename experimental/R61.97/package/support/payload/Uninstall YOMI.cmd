@echo off
setlocal
title Uninstall YOMI

set "SCRIPT=%~dp0app\uninstall.ps1"

if not exist "%SCRIPT%" (
    echo.
    echo YOMI uninstaller script was not found:
    echo "%SCRIPT%"
    echo.
    pause
    exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%"
set "RC=%ERRORLEVEL%"

if not "%RC%"=="0" (
    echo.
    echo YOMI uninstaller exited with code %RC%.
    echo.
    pause
)

exit /b %RC%
