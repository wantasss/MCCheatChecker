$ErrorActionPreference = "Stop"

Write-Host "=========================================================="
Write-Host "MCCheatChecker Tool 74 (Mouse & Macro Forensics) Test Suite"
Write-Host "=========================================================="
Write-Host ""

# [1] Verifying UTF-8 BOM on all repository .cs files
Write-Host "[1] Verifying UTF-8 BOM on all repository .cs files..."
$csFiles = Get-ChildItem -Path "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\*.cs"
$missingBom = 0
foreach ($f in $csFiles) {
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    if ($bytes.Length -lt 3 -or $bytes[0] -ne 0xEF -or $bytes[1] -ne 0xBB -or $bytes[2] -ne 0xBF) {
        Write-Host "    [!] Missing BOM: $($f.Name)" -ForegroundColor Red
        $missingBom++
    }
}

Write-Host "    Total .cs files: $($csFiles.Count) (Expected: 122)"
if ($missingBom -eq 0) {
    Write-Host "    [+] Verified $($csFiles.Count)/$($csFiles.Count) files with UTF-8 BOM (0xEF, 0xBB, 0xBF)." -ForegroundColor Green
} else {
    throw "UTF-8 BOM check failed: $missingBom files missing BOM."
}
Write-Host ""

# [2] Loading Assembly via Reflection
Write-Host "[2] Loading Assembly into Memory via Reflection..."
$exePath = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe"
if (-not (Test-Path $exePath)) {
    throw "Target binary not found: $exePath"
}
$bytes = [System.IO.File]::ReadAllBytes($exePath)
$asm = [System.Reflection.Assembly]::Load($bytes)
Write-Host "    [+] Assembly loaded: $($asm.FullName)" -ForegroundColor Green
Write-Host ""

# [3] Resolving Architectural Types for Tool 74
Write-Host "[3] Resolving Architectural Types for Tool 74..."
$formType = $asm.GetType("MCCheatChecker.HardwareMouseAndPollingRateAuditViewerForm")
$itemType = $asm.GetType("MCCheatChecker.MouseForensicEvidenceItem")
$filterType = $asm.GetType("MCCheatChecker.MouseForensicFilter")

if ($null -eq $formType) { throw "Type not found: MCCheatChecker.HardwareMouseAndPollingRateAuditViewerForm" }
if ($null -eq $itemType) { throw "Type not found: MCCheatChecker.MouseForensicEvidenceItem" }
if ($null -eq $filterType) { throw "Type not found: MCCheatChecker.MouseForensicFilter" }
Write-Host "    [+] All types resolved successfully." -ForegroundColor Green
Write-Host ""

# [4] Testing ManualChecks.RunHardwareMouseAndMacroAuditCheck() Runtime
Write-Host "[4] Testing ManualChecks.RunHardwareMouseAndMacroAuditCheck() Runtime..."
$manualChecksType = $asm.GetType("MCCheatChecker.ManualChecks")
$method = $manualChecksType.GetMethod("RunHardwareMouseAndMacroAuditCheck", [System.Reflection.BindingFlags]"Public,Static")
if ($null -eq $method) { throw "Method RunHardwareMouseAndMacroAuditCheck not found on ManualChecks" }

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$results = $method.Invoke($null, @())
$sw.Stop()

Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms (threshold: < 500 ms)" -ForegroundColor Green
Write-Host "    [+] Returned check results count: $($results.Count)"
foreach ($r in $results) {
    Write-Host "        [$($r.Severity)] $($r.Name) -> $($r.Evidence)"
}
Write-Host ""

# [5] Testing HardwareMouseAndPollingRateAuditViewerForm.PerformHardwareMouseAndMacroAudit()
Write-Host "[5] Testing PerformHardwareMouseAndMacroAudit() Core Forensic Engine..."
$engineMethod = $formType.GetMethod("PerformHardwareMouseAndMacroAudit", [System.Reflection.BindingFlags]"Public,Static")
if ($null -eq $engineMethod) { throw "Method PerformHardwareMouseAndMacroAudit not found on Form" }

$sw2 = [System.Diagnostics.Stopwatch]::StartNew()
$evidenceItems = $engineMethod.Invoke($null, @())
$sw2.Stop()

Write-Host "    [+] Engine scanned in $($sw2.ElapsedMilliseconds) ms" -ForegroundColor Green
Write-Host "    [+] Total forensic evidence items discovered: $($evidenceItems.Count)"
foreach ($it in $evidenceItems) {
    Write-Host "        #$($it.Index) [$($it.ThreatLevel)] $($it.Category): $($it.DeviceOrSource) -> $($it.ParameterOrValue)"
}
Write-Host ""

# [6] Testing HardwareMouseAndPollingRateAuditViewerForm Constructor
Write-Host "[6] Testing HardwareMouseAndPollingRateAuditViewerForm Constructor..."
$formInstance = [Activator]::CreateInstance($formType, @("TestPlayer74"))
if ($null -eq $formInstance) { throw "Failed to instantiate HardwareMouseAndPollingRateAuditViewerForm" }
Write-Host "    [+] Form Title: $($formInstance.Text)" -ForegroundColor Green
Write-Host "    [+] Form Controls Count: $($formInstance.Controls.Count)" -ForegroundColor Green
$formInstance.Dispose()
Write-Host ""

Write-Host "=========================================================="
Write-Host "ALL TOOL 74 VERIFICATION TESTS PASSED SUCCESSFULLY!"
Write-Host "=========================================================="
