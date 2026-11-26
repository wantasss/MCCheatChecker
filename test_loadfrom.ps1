$path = 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe'
try {
    $asm = [System.Reflection.Assembly]::LoadFrom($path)
    Write-Host "SUCCESS: Loaded $($asm.FullName)"
} catch {
    Write-Host "FAILED: $_"
}
