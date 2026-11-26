$url = 'https://docs.google.com/document/d/17wef3a0XQa-jDODl8nkkzwHWNsIro5HEoV8sk5eqPTc/preview'
$resp = Invoke-WebRequest -Uri $url -UseBasicParsing
Write-Host "Preview length: $($resp.Content.Length)"
[System.IO.File]::WriteAllText('C:\Users\Wanta\Desktop\MCCheatChecker\doc_preview.html', $resp.Content, [System.Text.Encoding]::UTF8)

# Check if tab title or 18b83l6en7uj appears in html
if ($resp.Content.Contains('18b83l6en7uj')) {
    Write-Host "Found tab id 18b83l6en7uj in HTML!"
} else {
    Write-Host "Tab id not in preview HTML"
}
