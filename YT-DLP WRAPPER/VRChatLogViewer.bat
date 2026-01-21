@echo off
title VRChat Log Viewer

REM This BAT calls VRChatLogViewer.ps1 and
REM watches the newest output_log_*.txt in real time.

setlocal

REM Use UTF-8 code page in this console
chcp 65001 >nul

REM Move to the script folder
cd /d "%~dp0"

REM Run PowerShell script (UTF-8 friendly)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0VRChatLogViewer.ps1"

echo.
echo Log viewer exited. Press any key to close this window...
pause >nul

endlocal
