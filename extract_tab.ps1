$html = [System.IO.File]::ReadAllText('C:\Users\Wanta\Desktop\MCCheatChecker\doc_preview.html', [System.Text.Encoding]::UTF8)
$idx = $html.IndexOf('18b83l6en7uj')
while ($idx -ge 0) {
    Write-Host "--- Occurrence at $idx ---" -ForegroundColor Cyan
    $start = [Math]::Max(0, $idx - 300)
    $len = [Math]::Min(1200, $html.Length - $start)
    Write-Host $html.Substring($start, $len)
    $idx = $html.IndexOf('18b83l6en7uj', $idx + 1)
}
