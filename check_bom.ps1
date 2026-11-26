$files = @(
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\TimeAnomalyAndSystemClockTamperingViewerForm.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ManualChecks.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ForensicHubForm.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ModeratorChecklistForm.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MainForm.cs',
    'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ForensicDossierReportGeneratorForm.cs'
)
foreach ($f in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($f)
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    Write-Host "$([System.IO.Path]::GetFileName($f)) : BOM = $hasBom"
}
