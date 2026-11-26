$asm = [System.Reflection.Assembly]::LoadFrom("C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe")

Write-Host "=== TEST 1: SIGNATURE AUTO-UPDATE MANAGER ==="
$suType = $asm.GetType("MCCheatChecker.SignatureAutoUpdateManager")
if ($suType -eq $null) {
    Write-Error "SignatureAutoUpdateManager type not found!"
    exit 1
}
Write-Host "[PASS] Found Type: $($suType.FullName)"

$initMethod = $suType.GetMethod("Initialize")
$initMethod.Invoke($null, @())
$verProp = $suType.GetProperty("CurrentVersion")
Write-Host "[PASS] Signatures Version: $($verProp.GetValue($null, $null))"

$blMethod = $suType.GetMethod("IsBlacklistedJavaAgent")
$isVapeBlacklisted = $blMethod.Invoke($null, @("-javaagent:C:\Temp\vape.jar"))
if (-not $isVapeBlacklisted) {
    Write-Error "Vape was not recognized as blacklisted Java agent!"
    exit 1
}
Write-Host "[PASS] Vape correctly recognized as blacklisted Java Agent: $isVapeBlacklisted"

$wlMethod = $suType.GetMethod("IsWhitelistedJavaAgent")
$isFabricWhitelisted = $wlMethod.Invoke($null, @("-javaagent:libraries\fabric-loader.jar"))
if (-not $isFabricWhitelisted) {
    Write-Error "Fabric-loader was not recognized as whitelisted Java agent!"
    exit 1
}
Write-Host "[PASS] Fabric loader correctly recognized as whitelisted: $isFabricWhitelisted"

Write-Host "`n=== TEST 2: REFLECTION AND FORM CREATION (TOOL 83) ==="
$formType = $asm.GetType("MCCheatChecker.JavaAgentAndJvmFlagsViewerForm")
if ($formType -eq $null) {
    Write-Error "JavaAgentAndJvmFlagsViewerForm type not found!"
    exit 1
}
$form = [Activator]::CreateInstance($formType, @("TestTargetPlayer"))
Write-Host "[PASS] Successfully instantiated JavaAgentAndJvmFlagsViewerForm. Title: $($form.Text)"

Write-Host "`n=== TEST 3: RUN MANUALCHECKS.RUNJAVAAGENTANDJVMFLAGSCHECK ==="
$mcType = $asm.GetType("MCCheatChecker.ManualChecks")
$method = $mcType.GetMethod("RunJavaAgentAndJvmFlagsCheck")
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$results = $method.Invoke($null, @())
$sw.Stop()
Write-Host "[PASS] RunJavaAgentAndJvmFlagsCheck executed in $($sw.ElapsedMilliseconds) ms"
Write-Host "Total findings returned: $($results.Count)"
foreach ($r in $results) {
    Write-Host " - [$($r.Severity)] $($r.Rule) | $($r.Name) | $($r.Detail)"
}

Write-Host "`n=== TEST 4: WIRING IN FORENSICHUBFORM ==="
$hubType = $asm.GetType("MCCheatChecker.ForensicHubForm")
$hub = [Activator]::CreateInstance($hubType, [object[]]@("TestPlayer", "Moderator"))
$allToolsProp = $hubType.GetField("allTools", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
$tools = $allToolsProp.GetValue($hub)
$tool83 = $null
foreach ($t in $tools) {
    if ($t.Hotkey -eq "Ctrl+Alt+J") {
        $tool83 = $t
        break
    }
}
if ($tool83 -eq $null) {
    Write-Error "Tool 83 (Ctrl+Alt+J) not found in ForensicHubForm.allTools!"
    exit 1
}
Write-Host "[PASS] ForensicHubForm contains Tool 83: $($tool83.Name) with Hotkey $($tool83.Hotkey)"
Write-Host "Total tools in Hub: $($tools.Count)"

Write-Host "`n=== TEST 5: MODERATOR CHECKLIST STEP 80 ==="
$chkType = $asm.GetType("MCCheatChecker.ModeratorChecklistForm")
$chk = [Activator]::CreateInstance($chkType, [object[]]@("TestPlayer", "Moderator", $null, $null, $null, $null))
$stepsField = $chkType.GetField("steps", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
$steps = $stepsField.GetValue($chk)
$step80 = $null
foreach ($s in $steps) {
    if ($s.Number -eq 80) {
        $step80 = $s
        break
    }
}
if ($step80 -eq $null) {
    Write-Error "Step 80 not found in ModeratorChecklistForm.steps!"
    exit 1
}
Write-Host "[PASS] ModeratorChecklistForm contains Step 80: $($step80.Title)"

Write-Host "`n=== ALL UNIT TESTS FOR TOOL 83 & UPDATER PASSED SUCCESSFULLY ==="
