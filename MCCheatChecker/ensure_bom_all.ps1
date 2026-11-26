$files = Get-ChildItem 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\*.cs'
foreach ($f in $files) {
    & 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ensure_bom.ps1' -path $f.FullName
}
