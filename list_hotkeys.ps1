$lines = Select-String -Path 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\MainForm.cs' -Pattern 'e\.Control && e\.Alt && e\.KeyCode == Keys\.'
foreach ($l in $lines) {
    Write-Host $l.Line.Trim()
}
