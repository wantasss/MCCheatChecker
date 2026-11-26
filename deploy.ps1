Get-Process -Name 'MCCheatChecker' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500
$src = 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe'
$dst = 'C:\Users\Wanta\Desktop\MCCheatChecker.exe'
Copy-Item -Path $src -Destination $dst -Force
$fi = Get-Item $dst
Write-Host "Deployed: $($fi.Length) bytes, $($fi.LastWriteTime)"
