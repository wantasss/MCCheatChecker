$text = [System.IO.File]::ReadAllText('C:\Users\Wanta\Desktop\MCCheatChecker\extracted_manual_all_tabs.txt', [System.Text.Encoding]::UTF8)

$startIdx = $text.IndexOf("Анализ процесса javaw")
$endIdx = $text.IndexOf("ExecutedProgramList", $startIdx)
if ($endIdx -lt 0) { $endIdx = $text.Length }

Write-Host "Extracting from $startIdx to $endIdx (Total chars: $($endIdx - $startIdx))"
$section = $text.Substring($startIdx, $endIdx - $startIdx)
[System.IO.File]::WriteAllText('C:\Users\Wanta\Desktop\MCCheatChecker\system_informer_manual_full.txt', $section, [System.Text.Encoding]::UTF8)
Write-Host $section
