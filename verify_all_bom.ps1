$csFiles = Get-ChildItem -Path 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\*.cs'
$fixed = 0
$total = $csFiles.Count
foreach ($f in $csFiles) {
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    if ($bytes.Length -lt 3 -or $bytes[0] -ne 0xEF -or $bytes[1] -ne 0xBB -or $bytes[2] -ne 0xBF) {
        $bom = [byte[]]@(0xEF, 0xBB, 0xBF)
        $newBytes = New-Object byte[] ($bytes.Length + 3)
        [System.Array]::Copy($bom, 0, $newBytes, 0, 3)
        [System.Array]::Copy($bytes, 0, $newBytes, 3, $bytes.Length)
        [System.IO.File]::WriteAllBytes($f.FullName, $newBytes)
        Write-Host "[+] Applied BOM to: $($f.Name)"
        $fixed++
    }
}
Write-Host "[=] Total .cs files: $total | Files updated with BOM: $fixed"
