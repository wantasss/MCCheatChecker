$urlTxt = 'https://docs.google.com/document/d/1p7UycItJzY_F4Fiv0NjGYBkgK5Usm5UjCOPeqcFiPSI/export?format=txt'
$urlPrev = 'https://docs.google.com/document/d/1p7UycItJzY_F4Fiv0NjGYBkgK5Usm5UjCOPeqcFiPSI/preview'

try {
    $r1 = Invoke-WebRequest -Uri $urlTxt -UseBasicParsing -TimeoutSec 15
    [System.IO.File]::WriteAllText('C:\Users\Wanta\Desktop\MCCheatChecker\regex_mods_doc.txt', $r1.Content, [System.Text.Encoding]::UTF8)
    Write-Host "TXT length: $($r1.Content.Length)"
} catch {
    Write-Host "TXT error: $($_.Exception.Message)"
}

try {
    $r2 = Invoke-WebRequest -Uri $urlPrev -UseBasicParsing -TimeoutSec 15
    [System.IO.File]::WriteAllText('C:\Users\Wanta\Desktop\MCCheatChecker\regex_mods_preview.html', $r2.Content, [System.Text.Encoding]::UTF8)
    Write-Host "HTML length: $($r2.Content.Length)"
    $regex = [System.Text.RegularExpressions.Regex]::new('\{"ty":"is","ibi":\d+,"s":"([^"]+)"\}')
    $ms = $regex.Matches($r2.Content)
    $sb = New-Object System.Text.StringBuilder
    foreach ($m in $ms) {
        $sb.AppendLine([System.Text.RegularExpressions.Regex]::Unescape($m.Groups[1].Value)) | Out-Null
    }
    [System.IO.File]::WriteAllText('C:\Users\Wanta\Desktop\MCCheatChecker\regex_mods_extracted.txt', $sb.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Extracted HTML text length: $($sb.Length)"
} catch {
    Write-Host "HTML error: $($_.Exception.Message)"
}
