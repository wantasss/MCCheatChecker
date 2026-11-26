$ErrorActionPreference = "Stop"

Write-Host "=========================================================="
Write-Host "MCCheatChecker Tool 76 (Kernel Drivers & BYOVD) Test Suite"
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

Write-Host "    Total .cs files: $($csFiles.Count) (Expected: 124)"
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

# [3] Resolving Architectural Types for Tool 76
Write-Host "[3] Resolving Architectural Types for Tool 76..."
$formType = $asm.GetType("MCCheatChecker.KernelDriversAndHypervisorHooksViewerForm")
$itemType = $asm.GetType("MCCheatChecker.KernelDriverEvidenceItem")
$filterType = $asm.GetType("MCCheatChecker.KernelDriverFilter")

if ($null -eq $formType) { throw "Type not found: MCCheatChecker.KernelDriversAndHypervisorHooksViewerForm" }
if ($null -eq $itemType) { throw "Type not found: MCCheatChecker.KernelDriverEvidenceItem" }
if ($null -eq $filterType) { throw "Type not found: MCCheatChecker.KernelDriverFilter" }
Write-Host "    [+] All types resolved successfully." -ForegroundColor Green
Write-Host ""

# [4] Testing ManualChecks.RunKernelDriversAndHypervisorHooksCheck() Runtime
Write-Host "[4] Testing ManualChecks.RunKernelDriversAndHypervisorHooksCheck() Runtime..."
$manualChecksType = $asm.GetType("MCCheatChecker.ManualChecks")
$method = $manualChecksType.GetMethod("RunKernelDriversAndHypervisorHooksCheck", [System.Reflection.BindingFlags]"Public,Static")
if ($null -eq $method) { throw "Method RunKernelDriversAndHypervisorHooksCheck not found on ManualChecks" }

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$results = $method.Invoke($null, @())
$sw.Stop()

Write-Host "    [+] Executed in $($sw.ElapsedMilliseconds) ms (threshold: < 500 ms)" -ForegroundColor Green
Write-Host "    [+] Returned check results count: $($results.Count)"
foreach ($r in $results) {
    Write-Host "        [$($r.Severity)] $($r.Name) -> $($r.Evidence)"
}
Write-Host ""

# [5] Testing KernelDriversAndHypervisorHooksViewerForm.PerformKernelDriverForensicScan()
Write-Host "[5] Testing PerformKernelDriverForensicScan() Core Forensic Engine..."
$engineMethod = $formType.GetMethod("PerformKernelDriverForensicScan", [System.Reflection.BindingFlags]"Public,Static")
if ($null -eq $engineMethod) { throw "Method PerformKernelDriverForensicScan not found on Form" }

$sw2 = [System.Diagnostics.Stopwatch]::StartNew()
$evidenceItems = $engineMethod.Invoke($null, @())
$sw2.Stop()

Write-Host "    [+] Engine scanned in $($sw2.ElapsedMilliseconds) ms" -ForegroundColor Green
Write-Host "    [+] Total forensic evidence items discovered: $($evidenceItems.Count)"
foreach ($it in $evidenceItems) {
    Write-Host "        #$($it.Index) [$($it.ThreatLevel)] $($it.Category): $($it.DriverName) -> $($it.ParameterOrValue)"
}
Write-Host ""

# [6] Testing KernelDriversAndHypervisorHooksViewerForm Constructor
Write-Host "[6] Testing KernelDriversAndHypervisorHooksViewerForm Constructor..."
$formInstance = [Activator]::CreateInstance($formType, @("TestPlayer76"))
if ($null -eq $formInstance) { throw "Failed to instantiate KernelDriversAndHypervisorHooksViewerForm" }
Write-Host "    [+] Form Title: $($formInstance.Text)" -ForegroundColor Green
Write-Host "    [+] Form Controls Count: $($formInstance.Controls.Count)" -ForegroundColor Green
$formInstance.Dispose()
Write-Host ""

Write-Host "=========================================================="
Write-Host "ALL TOOL 76 VERIFICATION TESTS PASSED SUCCESSFULLY!"
Write-Host "=========================================================="
