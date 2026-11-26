$files = Get-ChildItem -Path 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker' -Filter '*.cs'
foreach ($f in $files) {
    $matches = Select-String -Path $f.FullName -Pattern 'WWE'
    if ($matches) {
        foreach ($m in $matches) {
            Write-Host "$($f.Name):$($m.LineNumber): $($m.Line.Trim())"
        }
    }
}
