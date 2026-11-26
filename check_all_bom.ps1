$nonBom = 0
$total = 0
Get-ChildItem -Path "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker" -Filter "*.cs" | ForEach-Object {
    $total++
    $bytes = [System.IO.File]::ReadAllBytes($_.FullName)
    if ($bytes.Length -lt 3 -or $bytes[0] -ne 0xEF -or $bytes[1] -ne 0xBB -or $bytes[2] -ne 0xBF) {
        Write-Host "Missing BOM: $($_.Name)"
        $nonBom++
    }
}
Write-Host "Verified $total files. Total non-BOM .cs files: $nonBom"
