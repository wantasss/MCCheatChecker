$text = [System.IO.File]::ReadAllText('C:\Users\Wanta\Desktop\MCCheatChecker\extracted_manual_all_tabs.txt', [System.Text.Encoding]::UTF8)

$lines = $text -split "`r?`n"
Write-Host "Total lines: $($lines.Length)"

for ($i = 0; $i -lt $lines.Length; $i++) {
    $l = $lines[$i].Trim()
    if ($l.Length -gt 3 -and $l.Length -lt 60) {
        if ($l.StartsWith("Анализ") -or $l.StartsWith("Проверка") -or $l.StartsWith("Как") -or $l.StartsWith("Уровень") -or $l.EndsWith("уровень") -or $l.Contains("уровень") -or $l.StartsWith("Глава") -or $l.StartsWith("Раздел")) {
            Write-Host "[$i] $l" -ForegroundColor Yellow
        }
    }
}
