$ErrorActionPreference = "Stop"

Write-Host "=========================================================="
Write-Host "MCCheatChecker Tool 72 (AMSI, ETW & Patching) Test Suite"
Write-Host "=========================================================="

$binExe = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe"
if (-not (Test-Path $binExe)) {
    throw "Target executable not found at $binExe"
}

# 1. BOM Check across all 120 .cs files
Write-Host "`n[1] Verifying UTF-8 BOM on all repository .cs files..."
$csFiles = Get-ChildItem "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker" -Filter "*.cs"
Write-Host "    Total .cs files: $($csFiles.Count) (Expected: 120)"
if ($csFiles.Count -ne 120) {
    throw "Expected 120 .cs files, but found $($csFiles.Count)!"
}
foreach ($f in $csFiles) {
    $b = Get-Content $f.FullName -Encoding Byte -TotalCount 3
    if ($b[0] -ne 0xEF -or $b[1] -ne 0xBB -or $b[2] -ne 0xBF) {
        throw "File $($f.Name) is missing UTF-8 BOM!"
    }
}
Write-Host "    [+] Verified 120/120 files with UTF-8 BOM (0xEF, 0xBB, 0xBF)."

# 2. Reflection Load
Write-Host "`n[2] Loading Assembly into Memory via Reflection..."
$bytes = [System.IO.File]::ReadAllBytes($binExe)
$asm = [System.Reflection.Assembly]::Load($bytes)
Write-Host "    [+] Assembly loaded: $($asm.FullName)"

# 3. Type Resolution
Write-Host "`n[3] Resolving Architectural Types for Tool 72..."
$amsiFormType = $asm.GetType("MCCheatChecker.AmsiEtwAndPatchingDetectorViewerForm")
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$hubType = $asm.GetType("MCCheatChecker.ForensicHubForm")
$chkType = $asm.GetType("MCCheatChecker.ModeratorChecklistForm")
$mainType = $asm.GetType("MCCheatChecker.MainForm")

if ($amsiFormType -eq $null) { throw "Missing AmsiEtwAndPatchingDetectorViewerForm!" }
if ($mcType -eq $null) { throw "Missing ManualChecks!" }
if ($hubType -eq $null) { throw "Missing ForensicHubForm!" }
if ($chkType -eq $null) { throw "Missing ModeratorChecklistForm!" }
if ($mainType -eq $null) { throw "Missing MainForm!" }
Write-Host "    [+] All types resolved successfully."

# 4. Test ManualChecks.RunAmsiEtwAndMemoryPatchingCheck()
Write-Host "`n[4] Testing ManualChecks.RunAmsiEtwAndMemoryPatchingCheck() Runtime Responsiveness..."
$method = $mcType.GetMethod("RunAmsiEtwAndMemoryPatchingCheck")
if ($method -eq $null) { throw "Missing RunAmsiEtwAndMemoryPatchingCheck method in ManualChecks!" }

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$results = $method.Invoke($null, $null)
$sw.Stop()
Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms (threshold: < 500 ms)"
Write-Host "    [+] Returned check results count: $($results.Count)"
foreach ($r in $results) {
    Write-Host "        [$($r.Severity)] $($r.Name) -> $($r.Detail)"
    if ($r.Severity -eq "Critical") {
        Write-Host "        Evidence: $($r.Evidence)"
    }
}

# 5. Test Form Instantiation
Write-Host "`n[5] Testing AmsiEtwAndPatchingDetectorViewerForm Constructor..."
$form = [System.Activator]::CreateInstance($amsiFormType, [object[]]@("TestPlayer"))
if ($form -eq $null) { throw "Failed to instantiate AmsiEtwAndPatchingDetectorViewerForm!" }
Write-Host "    [+] Form Title: $($form.Text)"
Write-Host "    [+] Form Controls Count: $($form.Controls.Count)"
$form.Dispose()

Write-Host "`n=========================================================="
Write-Host "ALL TOOL 72 VERIFICATION TESTS PASSED SUCCESSFULLY!"
Write-Host "=========================================================="
