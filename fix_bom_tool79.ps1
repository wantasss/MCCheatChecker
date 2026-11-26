$f = 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\UsoAndWindowsUpdateTamperingViewerForm.cs'
$bytes = [System.IO.File]::ReadAllBytes($f)
$hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
Write-Host "UsoAndWindowsUpdateTamperingViewerForm.cs : BOM = $hasBom"
if (-not $hasBom) {
    $utf8WithBom = New-Object System.Text.UTF8Encoding($true)
    $text = [System.IO.File]::ReadAllText($f, [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllText($f, $text, $utf8WithBom)
    Write-Host "Added BOM. New BOM status: True"
}
