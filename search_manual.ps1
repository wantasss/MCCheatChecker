$text = [System.IO.File]::ReadAllText('C:\Users\Wanta\Desktop\MCCheatChecker\manual_system.txt', [System.Text.Encoding]::UTF8)

# Find sections mentioning System or систем
$lines = $text -split "`r?`n"
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match "system|систем|SystemDLC|systemdlc") {
        Write-Host "--- Line $i ---" -ForegroundColor Cyan
        $start = [Math]::Max(0, $i - 3)
        $end = [Math]::Min($lines.Length - 1, $i + 15)
        for ($j = $start; $j -le $end; $j++) {
            Write-Host $lines[$j]
        }
    }
}
