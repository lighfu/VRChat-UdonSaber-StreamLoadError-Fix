@echo off
setlocal enabledelayedexpansion

set "TOOLS_DIR=%APPDATA%\..\LocalLow\VRChat\VRChat\Tools"
for %%I in ("%TOOLS_DIR%") do set "TOOLS_DIR=%%~fI"

if not exist "%TOOLS_DIR%\." (
    echo [ERROR] Target directory not found: "%TOOLS_DIR%"
    pause
    exit /b 1
)

set "WRAPPER_PATH=%TOOLS_DIR%\yt-dlp.exe"
set "ORIGINAL_BACKUP=%TOOLS_DIR%\yt-dlp_.exe"
set "WRAPPER_LOG=%TOOLS_DIR%\wrapper.log"

echo Uninstalling wrapper from "%TOOLS_DIR%"

set "WRAPPER_EXISTS=0"
set "BACKUP_EXISTS=0"

if exist "%WRAPPER_PATH%" set "WRAPPER_EXISTS=1"
if exist "%ORIGINAL_BACKUP%" set "BACKUP_EXISTS=1"

if "%WRAPPER_EXISTS%"=="1" if "%BACKUP_EXISTS%"=="1" (
    attrib -R "%WRAPPER_PATH%" >nul 2>&1
    attrib -R "%ORIGINAL_BACKUP%" >nul 2>&1
    attrib -R "%TOOLS_DIR%\yt-dlp.exe" >nul 2>&1

    del /Q "%WRAPPER_PATH%"
    if errorlevel 1 (
        echo [ERROR] Failed to delete wrapper yt-dlp.exe
        pause
        exit /b 1
    )
    echo Removed wrapper executable.

    ren "%ORIGINAL_BACKUP%" "yt-dlp.exe"
    if errorlevel 1 (
        echo [ERROR] Failed to restore original yt-dlp.exe
        pause
        exit /b 1
    )
    echo Restored original yt-dlp.exe.
) else (
    if not "%WRAPPER_EXISTS%"=="1" (
        echo Wrapper yt-dlp.exe not found; nothing to delete.
    )
    if not "%BACKUP_EXISTS%"=="1" (
        echo Backup yt-dlp_.exe not found; original file may already be restored.
    )
)

if exist "%WRAPPER_LOG%" (
    del /Q "%WRAPPER_LOG%"
    if errorlevel 1 (
        echo [ERROR] Failed to delete wrapper.log
        pause
        exit /b 1
    )
    echo Removed wrapper.log.
)

echo Uninstallation completed.
pause
exit /b 0

