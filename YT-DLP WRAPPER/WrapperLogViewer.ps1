Param()

# Force UTF-8 output so Japanese / emoji don't garble
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$OutputEncoding = New-Object System.Text.UTF8Encoding($false)

# 監視対象: %APPDATA%\..\LocalLow\VRChat\VRChat\Tools\wrapper.log
$baseDir   = Join-Path $env:APPDATA "..\LocalLow\VRChat\VRChat"
$baseDir   = Resolve-Path $baseDir -ErrorAction SilentlyContinue

if (-not $baseDir) {
    Write-Host "VRChat base directory was not found."
    Write-Host "Expected base path: $([IO.Path]::Combine($env:APPDATA, '..\LocalLow\VRChat\VRChat'))"
    Read-Host "Press ENTER to exit..."
    exit 1
}

$toolsDir  = Join-Path $baseDir.Path "Tools"
$logPath   = Join-Path $toolsDir "wrapper.log"

Write-Host "Target log file: $logPath"
Write-Host "Press Ctrl + C to stop."
Write-Host ""

# ファイルが現れるまで待機
while (-not (Test-Path $logPath)) {
    Write-Host "wrapper.log が見つかりません。作成されるのを待機中..."
    Start-Sleep -Seconds 3
}

# 現在の行数（既存ログは一度に出したくない場合は 0 のままにする）
$readLines = 0

while ($true) {
    try {
        if (-not (Test-Path $logPath)) {
            Write-Host "wrapper.log が削除または移動されました。再出現を待機します..."
            while (-not (Test-Path $logPath)) {
                Start-Sleep -Seconds 3
            }
            # ファイルが作り直された場合、先頭から読みたくないので行数をリセット
            $readLines = 0
            continue
        }

        $lines = Get-Content -Path $logPath -Encoding UTF8

        for ($i = $readLines; $i -lt $lines.Count; $i++) {
            Write-Host $lines[$i]
        }

        $readLines = $lines.Count
    }
    catch {
        Write-Host "An error occurred while reading wrapper.log: $($_.Exception.Message)" -ForegroundColor Red
    }

    Start-Sleep -Milliseconds 500
}
