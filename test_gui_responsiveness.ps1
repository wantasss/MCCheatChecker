$exePath = 'C:\Users\Wanta\Desktop\MCCheatChecker.exe'

Write-Host "=== Process Responsiveness Test ==="
$proc = Start-Process -FilePath $exePath -PassThru
Start-Sleep -Seconds 4

if ($proc.HasExited) {
    Write-Host "FAIL: Desktop exe exited immediately (code $($proc.ExitCode))"
    exit 1
} else {
    $proc.Refresh()
    if ($proc.Responding) {
        Write-Host "PASS: Desktop exe is running and responsive (PID=$($proc.Id))"
    } else {
        Write-Host "FAIL: Desktop exe not responding"
        exit 1
    }

    # Verify no rogue tools spawned
    $childProcs = Get-Process | Where-Object { $_.Id -ne $proc.Id -and $_.StartTime -gt (Get-Date).AddSeconds(-6) }
    $rogueCount = 0
    foreach ($cp in $childProcs) {
        if ($cp.ProcessName -ne 'dllhost' -and $cp.ProcessName -ne 'conhost' -and $cp.ProcessName -ne 'powershell' -and $cp.ProcessName -ne 'Discord' -and $cp.ProcessName -ne 'MCCheatChecker') {
            Write-Host "WARNING: Unexpected child process: $($cp.ProcessName) (PID=$($cp.Id))"
            $rogueCount++
        }
    }
    if ($rogueCount -eq 0) {
        Write-Host "PASS: 0 external tools launched on startup"
    } else {
        Write-Host "FAIL: Rogue processes launched"
        exit 1
    }

    $proc.Kill()
    Write-Host "Desktop exe process stopped."
}

Write-Host ""
Write-Host "=== RUNTIME RESPONSIVENESS & CLEAN STARTUP: PASSED (100%) ==="
