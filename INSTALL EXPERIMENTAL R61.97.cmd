@echo off
setlocal
cd /d "%~dp0"
title Install YOMI R61.97 Experimental

echo.
echo ================================================
echo        YOMI R61.97 EXPERIMENTAL INSTALLER
echo ================================================
echo.
echo This installs or upgrades to
echo YOMI 4.2.0.8 R61.97 experimental beta.
echo.
echo Existing YOMI settings and user data are preserved.
echo The installer verifies the package before installation.
echo.

set "INSTALLER=%~dp0experimental\R61.97\install-experimental-r61.97.ps1"
if not exist "%INSTALLER%" (
    echo ERROR: R61.97 installer is missing.
    echo Fully extract the branch ZIP before running this file.
    echo.
    pause
    exit /b 2
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%INSTALLER%"
set "EC=%ERRORLEVEL%"
echo.
if "%EC%"=="0" (
    echo R61.97 installation finished successfully.
) else (
    echo R61.97 installation failed with exit code %EC%.
    echo Log: %LOCALAPPDATA%\YOMI\install-experimental-r61.97.log
    echo.
    pause
)
exit /b %EC%
