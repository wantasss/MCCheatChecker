$asm = [System.Reflection.Assembly]::LoadFrom("C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe")

Write-Host "=== TEST 1: REFLECTION AND FORM CREATION (TOOL 81) ==="
$formType = $asm.GetType("MCCheatChecker.Direct3dD3d11HookAndEspOverlayViewerForm")
if ($formType -eq $null) {
    Write-Error "Direct3dD3d11HookAndEspOverlayViewerForm type not found in assembly!"
    exit 1
}
Write-Host "[PASS] Found Form Type: $($formType.FullName)"

$form = [Activator]::CreateInstance($formType, @("TestTargetPlayer"))
if ($form -eq $null) {
    Write-Error "Failed to instantiate Direct3dD3d11HookAndEspOverlayViewerForm!"
    exit 1
}
Write-Host "[PASS] Successfully instantiated Direct3dD3d11HookAndEspOverlayViewerForm. Title: $($form.Text)"

Write-Host "`n=== TEST 2: RUN MANUALCHECKS.RUNDIRECT3DHOOKANDESPOVERLAYCHECK ==="
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$method = $mcType.GetMethod("RunDirect3dHookAndEspOverlayCheck")
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$results = $method.Invoke($null, @())
$sw.Stop()
Write-Host "[PASS] RunDirect3dHookAndEspOverlayCheck executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "Total findings returned: $($results.Count)"
foreach ($r in $results) {
    Write-Host " - [$($r.Severity)] $($r.Rule) | $($r.Name) | $($r.Detail)"
}

Write-Host "`n=== TEST 3: WIRING IN FORENSICHUBFORM ==="
$hubType = $asm.GetType("MCCheatChecker.ForensicHubForm")
$hub = [Activator]::CreateInstance($hubType, [object[]]@("TestPlayer", "Moderator"))
$allToolsProp = $hubType.GetField("allTools", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
$tools = $allToolsProp.GetValue($hub)
$tool81 = $null
foreach ($t in $tools) {
    if ($t.Hotkey -eq "Ctrl+Alt+F") {
        $tool81 = $t
        break
    }
}
if ($tool81 -eq $null) {
    Write-Error "Tool 81 (Ctrl+Alt+F) not found in ForensicHubForm.allTools!"
    exit 1
}
Write-Host "[PASS] ForensicHubForm contains Tool 81: $($tool81.Name) with Hotkey $($tool81.Hotkey)"
Write-Host "Total tools in Hub: $($tools.Count)"

Write-Host "`n=== TEST 4: MODERATOR CHECKLIST STEP 78 ==="
$chkType = $asm.GetType("MCCheatChecker.ModeratorChecklistForm")
$chk = [Activator]::CreateInstance($chkType, [object[]]@("TestPlayer", "Moderator", $null, $null, $null, $null))
$stepsField = $chkType.GetField("steps", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
$steps = $stepsField.GetValue($chk)
$step78 = $null
foreach ($s in $steps) {
    if ($s.Number -eq 78) {
        $step78 = $s
        break
    }
}
if ($step78 -eq $null) {
    Write-Error "Step 78 not found in ModeratorChecklistForm.steps!"
    exit 1
}
Write-Host "[PASS] ModeratorChecklistForm contains Step 78: $($step78.Title)"

Write-Host "`n=== ALL UNIT TESTS FOR TOOL 81 PASSED SUCCESSFULLY ==="
