$url = 'https://docs.google.com/document/d/17wef3a0XQa-jDODl8nkkzwHWNsIro5HEoV8sk5eqPTc/export?format=txt'
try {
    $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
    $content = $resp.Content
    Write-Host "Downloaded Google Doc text, length: $($content.Length)"
    [System.IO.File]::WriteAllText('C:\Users\Wanta\Desktop\MCCheatChecker\manual_system.txt', $content, [System.Text.Encoding]::UTF8)
    $previewLen = [Math]::Min(2000, $content.Length)
    Write-Host $content.Substring(0, $previewLen)
} catch {
    Write-Host "Export error: $($_.Exception.Message)"
}
