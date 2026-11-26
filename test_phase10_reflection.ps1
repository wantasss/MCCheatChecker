$ErrorActionPreference = "Stop"
$exePath = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe"
$asm = [System.Reflection.Assembly]::LoadFrom($exePath)

$engineType = $asm.GetType("MCCheatChecker.SystemInformerForensicsEngine", $true)
$formType = $asm.GetType("MCCheatChecker.SystemInformerAndProcessHackerForensicsViewerForm", $true)
$hubType = $asm.GetType("MCCheatChecker.ForensicHubForm", $true)
$checklistType = $asm.GetType("MCCheatChecker.ModeratorChecklistForm", $true)
$detectorType = $asm.GetType("MCCheatChecker.CheatDetector", $true)

Write-Host "PASS: Engine type found: $($engineType.FullName)"
Write-Host "PASS: Form type found: $($formType.FullName)"

# Check both constructors on Tool 88 form
$c0 = $formType.GetConstructor([Type]::EmptyTypes)
$c1 = $formType.GetConstructor([type[]]@([string]))
if (-not $c0) { throw "Missing parameterless constructor on Form" }
if (-not $c1) { throw "Missing (string) constructor on Form" }
Write-Host "PASS: Both constructors verified"

# Instantiate Tool 88 form
$form = [Activator]::CreateInstance($formType)
Write-Host "PASS: Form instantiated, Title: '$($form.Text)'"
$form.Dispose()

# Instantiate ForensicHubForm and ModeratorChecklistForm
$hub = [Activator]::CreateInstance($hubType, @("Player", ""))
Write-Host "PASS: ForensicHubForm instantiated, Title: '$($hub.Text)'"
$hub.Dispose()

$checklist = [Activator]::CreateInstance($checklistType, @("Player", "", $null, $null, $null, $null))
Write-Host "PASS: ModeratorChecklistForm instantiated, Title: '$($checklist.Text)'"
$checklist.Dispose()

# Run SystemInformerForensicsEngine.RunScan(true)
$runScanMethod = $engineType.GetMethod("RunScan", [type[]]@([bool]))
if (-not $runScanMethod) { throw "Missing RunScan(bool) method" }
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$summary = $runScanMethod.Invoke($null, @($true))
$sw.Stop()

Write-Host "PASS: RunScan(true) completed in $($sw.ElapsedMilliseconds) ms"
Write-Host ("Summary: Total={0}, Violations={1}, CsrssAccessDenied={2}" -f $summary.Items.Count, $summary.ViolationCount, $summary.CsrssAccessDenied)
if ($summary.ViolationCount -ne 0) { throw "Expected 0 violations on clean PC, got $($summary.ViolationCount)" }

# Run full clean-PC CheatDetector.RunFullScan
$runFullMethod = $detectorType.GetMethod("RunFullScan")
$swFull = [System.Diagnostics.Stopwatch]::StartNew()
$report = $runFullMethod.Invoke($null, @("Player", [System.Threading.CancellationToken]::None))
$swFull.Stop()

$results = $report.Results
$totalResults = $results.Count
$detected = @($results | Where-Object { $_.Detected })
Write-Host ("FullScan: TotalResults={0}, TotalDetected={1}, ElapsedMs={2}" -f $totalResults, $detected.Count, $swFull.ElapsedMilliseconds)
if ($detected.Count -gt 0) {
    foreach ($d in $detected) {
        Write-Host ("DETECTED: [{0}] {1} - {2}" -f $d.Severity, $d.ModuleName, $d.Description)
    }
    throw "Expected 0 detected items on clean PC, got $($detected.Count)"
}

Write-Host "=== PHASE 10 REFLECTION & FULL SCAN VERIFICATION: PASSED ==="
