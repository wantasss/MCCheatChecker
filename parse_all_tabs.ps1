$html = [System.IO.File]::ReadAllText('C:\Users\Wanta\Desktop\MCCheatChecker\doc_preview.html', [System.Text.Encoding]::UTF8)

# Find all DOCS_modelChunk occurrences and extract text parts
$regex = [System.Text.RegularExpressions.Regex]::new('\{"ty":"is","ibi":\d+,"s":"([^"]+)"\}')
$matches = $regex.Matches($html)
Write-Host "Total text segments found: $($matches.Count)"

$fullText = New-Object System.Text.StringBuilder
foreach ($m in $matches) {
    $raw = $m.Groups[1].Value
    $decoded = [System.Text.RegularExpressions.Regex]::Unescape($raw)
    $fullText.AppendLine($decoded) | Out-Null
}

$extracted = $fullText.ToString()
[System.IO.File]::WriteAllText('C:\Users\Wanta\Desktop\MCCheatChecker\extracted_manual_all_tabs.txt', $extracted, [System.Text.Encoding]::UTF8)
Write-Host "Saved extracted text ($($extracted.Length) chars) to extracted_manual_all_tabs.txt"

# Find sections about System Informer, javaw, explorer, DCOMLaunch, PcaSvc, DPS, csrss
$keywords = @("System Infomer", "Process Hacker", "javaw", "explorer.exe", "DCOMLaunch", "PcaSvc", "DPS", "csrss")
foreach ($kw in $keywords) {
    $pos = $extracted.IndexOf($kw)
    if ($pos -ge 0) {
        Write-Host "=== Found keyword: $kw ===" -ForegroundColor Green
        $start = [Math]::Max(0, $pos - 100)
        $len = [Math]::Min(1500, $extracted.Length - $start)
        Write-Host $extracted.Substring($start, $len)
    }
}
