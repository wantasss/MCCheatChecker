$files = Get-ChildItem -Path "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker" -Filter "*.cs" -Recurse
$missing = @()
foreach ($f in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    if ($bytes.Length -lt 3 -or $bytes[0] -ne 0xEF -or $bytes[1] -ne 0xBB -or $bytes[2] -ne 0xBF) {
        $missing += $f.FullName
        Write-Host "Missing BOM: $($f.FullName)"
        $bom = [byte[]](0xEF, 0xBB, 0xBF)
        $newBytes = $bom + $bytes
        [System.IO.File]::WriteAllBytes($f.FullName, $newBytes)
        Write-Host "Added BOM to: $($f.FullName)"
    }
}
Write-Host "Total files checked: $($files.Count)"
Write-Host "Total missing BOM fixed: $($missing.Count)"
