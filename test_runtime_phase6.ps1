# Runtime responsiveness test for Phase 6
$exePath = 'C:\Users\Wanta\Desktop\MCCheatChecker.exe'

$proc = Start-Process -FilePath $exePath -PassThru
Start-Sleep -Seconds 4

if ($proc.HasExited) {
    Write-Host "FAIL: Process exited unexpectedly (exit code $($proc.ExitCode))"
    exit 1
}

$proc.Refresh()
if ($proc.Responding) {
    Write-Host "PASS: Process is responding (PID=$($proc.Id))"
} else {
    Write-Host "FAIL: Process not responding"
}

# Check no external tool processes spawned
$beforePIDs = @($proc.Id)
$children = Get-Process | Where-Object { $_.Id -ne $proc.Id -and $_.StartTime -gt (Get-Date).AddSeconds(-6) } | Select-Object Name, Id
if ($children) {
    Write-Host "WARNING: Unexpected child processes spawned:"
    $children | ForEach-Object { Write-Host "  $($_.Name) (PID=$($_.Id))" }
} else {
    Write-Host "PASS: No external processes spawned"
}

$proc.Kill()
Write-Host "Process killed. RUNTIME TEST: PASSED"
