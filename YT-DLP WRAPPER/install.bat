@echo off
setlocal enabledelayedexpansion

set "TOOLS_DIR=%APPDATA%\..\LocalLow\VRChat\VRChat\Tools"
for %%I in ("%TOOLS_DIR%") do set "TOOLS_DIR=%%~fI"

set "WRAPPER_DIR=%~dp0"
set "WRAPPER_EXE=%WRAPPER_DIR%yt-dlp.exe"

if not exist "%WRAPPER_EXE%" (
    echo [ERROR] Wrapper executable not found: "%WRAPPER_EXE%"
    pause
    exit /b 1
)

if not exist "%TOOLS_DIR%\." (
    echo [ERROR] Target directory not found: "%TOOLS_DIR%"
    pause
    exit /b 1
)

echo Installing wrapper to "%TOOLS_DIR%"

if exist "%TOOLS_DIR%\yt-dlp.exe" (
    echo Backing up existing yt-dlp.exe to yt-dlp_.exe
    ren "%TOOLS_DIR%\yt-dlp.exe" "yt-dlp_.exe"
    if errorlevel 1 (
        echo [ERROR] Failed to rename existing yt-dlp.exe
        pause
        exit /b 1
    )
)

copy /Y "%WRAPPER_EXE%" "%TOOLS_DIR%\yt-dlp.exe" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy wrapper executable.
    pause
    exit /b 1
)

attrib +R "%TOOLS_DIR%\yt-dlp.exe" >nul
if errorlevel 1 (
    echo [WARNING] Failed to set read-only attribute on yt-dlp.exe.
)

echo Installation completed successfully.
pause
exit /b 0

