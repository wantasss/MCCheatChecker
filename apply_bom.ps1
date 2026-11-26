param([string]$path)
$bytes = [System.IO.File]::ReadAllBytes($path)
if ($bytes[0] -ne 0xEF -or $bytes[1] -ne 0xBB -or $bytes[2] -ne 0xBF) {
    $bom = [byte[]]@(0xEF, 0xBB, 0xBF)
    $newBytes = New-Object byte[] ($bytes.Length + 3)
    [System.Array]::Copy($bom, 0, $newBytes, 0, 3)
    [System.Array]::Copy($bytes, 0, $newBytes, 3, $bytes.Length)
    [System.IO.File]::WriteAllBytes($path, $newBytes)
    Write-Host "[+] Applied UTF-8 BOM to $path"
} else {
    Write-Host "[=] Already has BOM: $path"
}
