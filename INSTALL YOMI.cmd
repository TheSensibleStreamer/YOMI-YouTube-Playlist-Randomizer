@echo off
setlocal
cd /d "%~dp0"
title Install YOMI

echo.
echo ========================================
echo           INSTALL YOMI 4.2.0.6
echo ========================================
echo       YouTube OBS Music Interface
echo.
echo This is the YOMI installer launcher.
echo.

set "INSTALLER=%~dp0installer\install.ps1"

if not exist "%INSTALLER%" (
    echo ERROR: The installer engine is missing.
    echo.
    echo Fully extract the YOMI ZIP, then run:
    echo     INSTALL YOMI.cmd
    echo.
    pause
    exit /b 2
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%INSTALLER%"
set "EC=%ERRORLEVEL%"

echo.
if "%EC%"=="0" (
    echo ========================================
    echo          YOMI INSTALL FINISHED
    echo ========================================
    echo.
    echo Program:  C:\Program Files\YOMI
    echo Settings: %LOCALAPPDATA%\YOMI
) else (
    echo ========================================
    echo           YOMI INSTALL FAILED
    echo ========================================
    echo Exit code: %EC%
    echo.
    echo ===== LAST INSTALLER STAGE =====
    if exist "%LOCALAPPDATA%\YOMI\install-stage.txt" type "%LOCALAPPDATA%\YOMI\install-stage.txt"
    echo.
    echo ===== FAILURE STATUS =====
    if exist "%LOCALAPPDATA%\YOMI\install-status.txt" type "%LOCALAPPDATA%\YOMI\install-status.txt"
    echo.
    echo ===== END OF INSTALL LOG =====
    if exist "%LOCALAPPDATA%\YOMI\install.log" powershell.exe -NoProfile -Command "Get-Content -LiteralPath $env:LOCALAPPDATA'\YOMI\install.log' -Tail 18"
)
echo.
pause
exit /b %EC%
