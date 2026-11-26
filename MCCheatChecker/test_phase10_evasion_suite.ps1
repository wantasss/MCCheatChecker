$ErrorActionPreference = "Stop"
$exePath = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe"
$asm = [System.Reflection.Assembly]::LoadFrom($exePath)
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Write-Host "1. Testing constructors of Tool 88..."
$formType = $asm.GetType("MCCheatChecker.SystemInformerAndProcessHackerForensicsViewerForm")
$f0 = [Activator]::CreateInstance($formType)
if ($null -eq $f0) { throw "Parameterless constructor returned null" }
$f0.Dispose()

$f1 = [Activator]::CreateInstance($formType, @("TestPlayer88"))
if ($null -eq $f1) { throw "Player constructor returned null" }

Write-Host "2. Running Tool 88 WinForms GUI lifecycle & async scan..."
$flagsNonPub = [System.Reflection.BindingFlags]"NonPublic,Instance"
$lvField = $formType.GetField("lvArtifacts", $flagsNonPub)
$lblStatusField = $formType.GetField("lblScanStatus", $flagsNonPub)
$btnRescanField = $formType.GetField("btnRescan", $flagsNonPub)
$btnAllField = $formType.GetField("btnFilterAll", $flagsNonPub)
$btnViolField = $formType.GetField("btnFilterViolations", $flagsNonPub)
$btnJavawStrField = $formType.GetField("btnFilterJavawStrings", $flagsNonPub)
$btnJavawModField = $formType.GetField("btnFilterJavawModules", $flagsNonPub)
$btnExpField = $formType.GetField("btnFilterExplorer", $flagsNonPub)
$btnDcomField = $formType.GetField("btnFilterDcomLaunch", $flagsNonPub)
$btnPcaField = $formType.GetField("btnFilterPcaSvc", $flagsNonPub)
$btnDpsField = $formType.GetField("btnFilterDps", $flagsNonPub)
$btnCsrssField = $formType.GetField("btnFilterCsrss", $flagsNonPub)
$lastSummaryField = $formType.GetField("lastSummary", $flagsNonPub)
$isScanningField = $formType.GetField("isScanning", $flagsNonPub)

$f1.Show()
$sw = [System.Diagnostics.Stopwatch]::StartNew()
while ($sw.ElapsedMilliseconds -lt 15000) {
    [System.Windows.Forms.Application]::DoEvents()
    $scanning = [bool]$isScanningField.GetValue($f1)
    $summary = $lastSummaryField.GetValue($f1)
    if (-not $scanning -and $null -ne $summary) {
        break
    }
    Start-Sleep -Milliseconds 100
}
[System.Windows.Forms.Application]::DoEvents()

$summary = $lastSummaryField.GetValue($f1)
if ($null -eq $summary) { throw "Tool 88 async scan did not complete within 15s!" }

$lv = $lvField.GetValue($f1)
$statusLbl = $lblStatusField.GetValue($f1)
Write-Host ("Tool 88 Scan Complete: Duration={0}ms | Total={1} | Violations={2} | ListViewRows={3} | Status='{4}'" -f $summary.ScanDurationMs, $summary.TotalArtifacts, $summary.ViolationCount, $lv.Items.Count, $statusLbl.Text)
Write-Host ("Filter Buttons: {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8}" -f `
    $btnAllField.GetValue($f1).Text, `
    $btnViolField.GetValue($f1).Text, `
    $btnJavawStrField.GetValue($f1).Text, `
    $btnJavawModField.GetValue($f1).Text, `
    $btnExpField.GetValue($f1).Text, `
    $btnDcomField.GetValue($f1).Text, `
    $btnPcaField.GetValue($f1).Text, `
    $btnDpsField.GetValue($f1).Text, `
    $btnCsrssField.GetValue($f1).Text)

if ($summary.ViolationCount -ne 0) { throw "Expected 0 violations on clean PC, got $($summary.ViolationCount)" }

# Test filtering click
$btnCsrssField.GetValue($f1).PerformClick()
[System.Windows.Forms.Application]::DoEvents()
Write-Host ("After Csrss filter click: ListViewRows={0}" -f $lv.Items.Count)

$btnAllField.GetValue($f1).PerformClick()
[System.Windows.Forms.Application]::DoEvents()
$f1.Close()
$f1.Dispose()

Write-Host "3. Testing ManualChecks.RunSystemInformerAndProcessHackerManualCheck()..."
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$mcMethod = $mcType.GetMethod("RunSystemInformerAndProcessHackerManualCheck", [System.Reflection.BindingFlags]"Public,Static")
$mcRes = $mcMethod.Invoke($null, $null)
foreach ($r in $mcRes) {
    Write-Host ("ManualCheck: Name='{0}' | Detected={1} | Severity={2}" -f $r.Name, $r.Detected, $r.Severity)
    if ($r.Detected -or $r.Severity.ToString() -ne "Info") {
        throw "ManualCheck flagged false positive: $($r.Detail) | $($r.Evidence)"
    }
}

Write-Host "4. Testing ForensicHubForm (88 tools), ModeratorChecklistForm (85 steps), ForensicDossierReportGeneratorForm (Check 36)..."
$hubType = $asm.GetType("MCCheatChecker.ForensicHubForm")
$hub = [Activator]::CreateInstance($hubType, @("TestPlayer", "TestMod"))
$allToolsField = $hubType.GetField("allTools", $flagsNonPub)
$toolsList = $allToolsField.GetValue($hub)
$hasTool88 = $false
foreach ($t in $toolsList) {
    if ($t.Hotkey -like "*Ctrl+Alt+8*") { $hasTool88 = $true; Write-Host "Found Hub Tool: $($t.Name) [$($t.Hotkey)]" }
}
if (-not $hasTool88) { throw "Tool 88 missing in ForensicHubForm" }
$hub.Dispose()

$chkType = $asm.GetType("MCCheatChecker.ModeratorChecklistForm")
$chk = [Activator]::CreateInstance($chkType, @("TestPlayer", "TestMod", $null, $null, $null, $null))
$stepsField = $chkType.GetField("steps", $flagsNonPub)
$stepsList = $stepsField.GetValue($chk)
Write-Host ("ModeratorChecklistForm total steps: {0}" -f $stepsList.Count)
$hasStep85 = $false
foreach ($st in $stepsList) {
    if ($st.Number -eq 85) { $hasStep85 = $true; Write-Host "Found Step 85: $($st.Title)" }
}
if (-not $hasStep85 -or $stepsList.Count -ne 85) { throw "Expected 85 steps with Step 85 in ModeratorChecklistForm" }
$chk.Dispose()

Write-Host "5. Checking refactored forms instantiation (WorldSeed, UserActivityTimeline, BrowserSearch, BrowserMessenger) & Forensics Dossier..."
$uatInst = [Activator]::CreateInstance($asm.GetType("MCCheatChecker.UserActivityTimelineForm"))
$uatInst.Dispose()
Write-Host "Instantiated OK: UserActivityTimelineForm"
foreach ($fn in @("WorldSeedAndChunkbaseForensicsViewerForm","BrowserSearchAndHistoryViewerForm","BrowserMessengerDownloadsViewerForm")) {
    $ft = $asm.GetType("MCCheatChecker.$fn")
    $inst = [Activator]::CreateInstance($ft, @("TestPlayer"))
    $inst.Dispose()
    Write-Host "Instantiated OK: $fn"
}
$dosInst = [Activator]::CreateInstance($asm.GetType("MCCheatChecker.ForensicDossierReportGeneratorForm"), @("TestPlayer", "TestMod"))
$dosInst.Dispose()
Write-Host "Instantiated OK: ForensicDossierReportGeneratorForm"

Write-Host "ALL PHASE 10 EVASION_SUITE TESTS PASSED!"
