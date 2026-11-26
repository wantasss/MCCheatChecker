$proc = Start-Process -FilePath "C:\Users\Wanta\Desktop\MCCheatChecker.exe" -PassThru
Start-Sleep -Seconds 2
$resp = $proc.Responding
$pidNum = $proc.Id
$proc.Kill()
Write-Host "[+] Binary responsive: $resp (PID: $pidNum)"
