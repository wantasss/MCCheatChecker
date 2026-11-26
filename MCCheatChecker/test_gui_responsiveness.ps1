$proc = Start-Process "C:\Users\Wanta\Desktop\MCCheatChecker.exe" -PassThru
Start-Sleep -Seconds 2
Write-Host "Process ID: $($proc.Id)"
Write-Host "Responding: $($proc.Responding)"
$proc.Kill()
