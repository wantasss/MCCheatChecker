$asm = [System.Reflection.Assembly]::LoadFrom("C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe")

Write-Host "=== TEST 1: REFLECTION AND FORM CREATION (TOOL 79) ==="
$formType = $asm.GetType("MCCheatChecker.UsoAndWindowsUpdateTamperingViewerForm")
if ($formType -eq $null) {
    Write-Error "UsoAndWindowsUpdateTamperingViewerForm type not found in assembly!"
    exit 1
}
Write-Host "[PASS] Found Form Type: $($formType.FullName)"

$form = [Activator]::CreateInstance($formType, @("TestTargetPlayer"))
if ($form -eq $null) {
    Write-Error "Failed to instantiate UsoAndWindowsUpdateTamperingViewerForm!"
    exit 1
}
Write-Host "[PASS] Successfully instantiated UsoAndWindowsUpdateTamperingViewerForm. Title: $($form.Text)"

Write-Host "`n=== TEST 2: RUN MANUALCHECKS.RUNUSOANDWINDOWSUPDATETAMPERINGCHECK ==="
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$method = $mcType.GetMethod("RunUsoAndWindowsUpdateTamperingCheck")
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$results = $method.Invoke($null, @())
$sw.Stop()
Write-Host "[PASS] RunUsoAndWindowsUpdateTamperingCheck executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "Total findings returned: $($results.Count)"
foreach ($r in $results) {
    Write-Host " - [$($r.Severity)] $($r.Rule) | $($r.Name) | $($r.Detail)"
}

Write-Host "`n=== TEST 3: WIRING IN FORENSICHUBFORM ==="
$hubType = $asm.GetType("MCCheatChecker.ForensicHubForm")
$hub = [Activator]::CreateInstance($hubType, [object[]]@("TestPlayer", "Moderator"))
$allToolsProp = $hubType.GetField("allTools", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
$tools = $allToolsProp.GetValue($hub)
$tool79 = $null
foreach ($t in $tools) {
    if ($t.Hotkey -eq "Ctrl+Alt+U") {
        $tool79 = $t
        break
    }
}
if ($tool79 -eq $null) {
    Write-Error "Tool 79 (Ctrl+Alt+U) not found in ForensicHubForm.allTools!"
    exit 1
}
Write-Host "[PASS] ForensicHubForm contains Tool 79: $($tool79.Name) with Hotkey $($tool79.Hotkey)"
Write-Host "Total tools in Hub: $($tools.Count)"

Write-Host "`n=== TEST 4: MODERATOR CHECKLIST STEP 76 ==="
$chkType = $asm.GetType("MCCheatChecker.ModeratorChecklistForm")
$chk = [Activator]::CreateInstance($chkType, [object[]]@("TestPlayer", "Moderator", $null, $null, $null, $null))
$stepsField = $chkType.GetField("steps", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
$steps = $stepsField.GetValue($chk)
$step76 = $null
foreach ($s in $steps) {
    if ($s.Number -eq 76) {
        $step76 = $s
        break
    }
}
if ($step76 -eq $null) {
    Write-Error "Step 76 not found in ModeratorChecklistForm.steps!"
    exit 1
}
Write-Host "[PASS] ModeratorChecklistForm contains Step 76: $($step76.Title)"

Write-Host "`n=== ALL UNIT TESTS FOR TOOL 79 PASSED SUCCESSFULLY ==="
