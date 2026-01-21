Param()

# Force UTF-8 output so Japanese / emoji don't garble
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$OutputEncoding = New-Object System.Text.UTF8Encoding($false)

function Write-ColoredLogLine {
    param(
        [string]$line
    )

    if ($line -match '\bError\b') {
        # Error: fixed red
        $color = 'Red'
        Write-Host $line -ForegroundColor $color
        return
    }
    elseif ($line -match '\bWarning\b') {
        # Warning: fixed yellow
        $color = 'Yellow'
        Write-Host $line -ForegroundColor $color
        return
    }
    elseif ($line -match '\bDebug\b') {
        # Debug: hash only the message part (exclude timestamp + level)
        # Example:
        # 2025.12.01 18:19:14 Debug      -  Measure Human Avatar Avatar isRemeasure:True
        # -> hash "Measure Human Avatar Avatar isRemeasure:True"
        $parts = $line -split ' -  ', 2
        if ($parts.Count -ge 2) {
            $hashBase = $parts[1]
        }
        else {
            $hashBase = $line
        }

        $hash = [Math]::Abs($hashBase.GetHashCode())

        # 32–125 の範囲で暗すぎない整数 RGB を生成
        $r = [int](32 + ($hash % 94))
        $g = [int](32 + (([int]($hash / 256)) % 94))
        $b = [int](32 + (([int]($hash / 65536)) % 94))

        # ANSI エスケープ (VT100) を使って 24bit RGB で着色
        $esc = [char]27
        Write-Host ("{0}[38;2;{1};{2};{3}m{4}{0}[0m" -f $esc, $r, $g, $b, $line)
        return
    }
    elseif ($line -match '\bInfo\b') {
        # Info: fixed white
        $color = 'White'
    }
    else {
        # Other: neutral gray
        $color = 'Gray'
    }

    Write-Host $line -ForegroundColor $color
}

# Get VRChat log directory
$logDir = Join-Path $env:APPDATA "..\LocalLow\VRChat\VRChat"
$logDir = Resolve-Path $logDir -ErrorAction SilentlyContinue

if (-not $logDir) {
    Write-Host "VRChat log directory was not found."
    Write-Host "Expected path: $([IO.Path]::Combine($env:APPDATA, '..\LocalLow\VRChat\VRChat'))"
    Read-Host "Press ENTER to exit..."
    exit 1
}

$logDir = $logDir.Path

Write-Host "Log directory: $logDir"

while ($true) {
    # Get the newest output_log_*.txt
    $latest = Get-ChildItem -Path $logDir -Filter "output_log_*.txt" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $latest) {
        Write-Host "No output_log_*.txt file found. Please start VRChat and try again."
        Start-Sleep -Seconds 5
        continue
    }

    Write-Host "Watching file: $($latest.FullName)"
    Write-Host "Press Ctrl + C to stop."
    Write-Host ""

    try {
        $currentFile = $latest.FullName
        $readLines = 0

        while ($true) {
            if (-not (Test-Path $currentFile)) {
                break
            }

            # Read all lines (UTF-8) and output only new ones
            $lines = Get-Content -Path $currentFile -Encoding UTF8

            for ($i = $readLines; $i -lt $lines.Count; $i++) {
                Write-ColoredLogLine $lines[$i]
            }

            $readLines = $lines.Count

            # Check if a newer log file has appeared; if so, switch to it
            $latestNow = Get-ChildItem -Path $logDir -Filter "output_log_*.txt" -File -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 1

            if ($latestNow -and $latestNow.FullName -ne $currentFile) {
                Write-Host ""
                Write-Host "Detected newer log file. Switching to: $($latestNow.FullName)"
                break
            }

            Start-Sleep -Milliseconds 500
        }
    }
    catch {
        Write-Host "An error occurred while watching the file: $($_.Exception.Message)"
    }

    Write-Host ""
    Write-Host "Watching stopped. Searching for the newest log file again..."
    Start-Sleep -Seconds 3
}
