$asm = [System.Reflection.Assembly]::LoadFrom("C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe")

Write-Host "=== TEST 1: REFLECTION AND FORM CREATION ==="
$formType = $asm.GetType("MCCheatChecker.TimeAnomalyAndSystemClockTamperingViewerForm")
if ($formType -eq $null) {
    Write-Error "TimeAnomalyAndSystemClockTamperingViewerForm type not found in assembly!"
    exit 1
}
Write-Host "[PASS] Found Form Type: $($formType.FullName)"

$form = [Activator]::CreateInstance($formType, @("TestTargetPlayer"))
if ($form -eq $null) {
    Write-Error "Failed to instantiate TimeAnomalyAndSystemClockTamperingViewerForm!"
    exit 1
}
Write-Host "[PASS] Successfully instantiated TimeAnomalyAndSystemClockTamperingViewerForm. Title: $($form.Text)"

Write-Host "=== TEST 2: RUN MANUALCHECKS.RUNTIMEANOMALYANDSYSTEMCLOCKTAMPERINGCHECK ==="
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$method = $mcType.GetMethod("RunTimeAnomalyAndSystemClockTamperingCheck")
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$results = $method.Invoke($null, @())
$sw.Stop()
Write-Host "[PASS] RunTimeAnomalyAndSystemClockTamperingCheck executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "Total findings returned: $($results.Count)"
foreach ($r in $results) {
    Write-Host " - [$($r.Severity)] $($r.Rule) | $($r.Name) | $($r.Detail)"
}

Write-Host "=== TEST 3: WIRING VERIFICATION ==="
$hubType = $asm.GetType("MCCheatChecker.ForensicHubForm")
$hub = [Activator]::CreateInstance($hubType, [object[]]@("TestPlayer", "Moderator"))
$allToolsProp = $hubType.GetField("allTools", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
$tools = $allToolsProp.GetValue($hub)
$tool77 = $null
foreach ($t in $tools) {
    if ($t.Hotkey -eq "Ctrl+Alt+T") {
        $tool77 = $t
        break
    }
}
if ($tool77 -eq $null) {
    Write-Error "Tool 77 (Ctrl+Alt+T) not found in ForensicHubForm.allTools!"
    exit 1
}
Write-Host "[PASS] ForensicHubForm contains Tool 77: $($tool77.Name) with Hotkey $($tool77.Hotkey)"
Write-Host "Total tools in Hub: $($tools.Count)"

Write-Host "=== TEST 4: MODERATOR CHECKLIST STEP 74 ==="
$chkType = $asm.GetType("MCCheatChecker.ModeratorChecklistForm")
$chk = [Activator]::CreateInstance($chkType, [object[]]@("TestPlayer", "Moderator", $null, $null, $null, $null))
$stepsField = $chkType.GetField("steps", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
$steps = $stepsField.GetValue($chk)
$step74 = $null
foreach ($s in $steps) {
    if ($s.Number -eq 74) {
        $step74 = $s
        break
    }
}
if ($step74 -eq $null) {
    Write-Error "Step 74 not found in ModeratorChecklistForm.steps!"
    exit 1
}
Write-Host "[PASS] ModeratorChecklistForm contains Step 74: $($step74.Title)"

Write-Host "=== ALL TESTS COMPLETED SUCCESSFULLY ==="
