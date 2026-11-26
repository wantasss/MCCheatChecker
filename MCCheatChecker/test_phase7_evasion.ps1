Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$exePath = "C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\bin\MCCheatChecker.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "MCCheatChecker.exe not found at $exePath"
    exit 1
}

Write-Host "--- Loading Assembly: $exePath ---" -ForegroundColor Cyan
$bytes = [System.IO.File]::ReadAllBytes($exePath)
$assembly = [System.Reflection.Assembly]::Load($bytes)

$types = @(
    "MCCheatChecker.ServerRulesProfileManager",
    "MCCheatChecker.MixinAndModJsonForensicsViewerForm",
    "MCCheatChecker.UnifiedForensicTimelineViewerForm",
    "MCCheatChecker.JavaAgentAndJvmFlagsViewerForm",
    "MCCheatChecker.SignatureAutoUpdateManager"
)

Write-Host "`n--- Checking Type Existence ---" -ForegroundColor Cyan
foreach ($tName in $types) {
    $t = $assembly.GetType($tName)
    if ($t -ne $null) {
        Write-Host "[PASS] Type found: $tName" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Type NOT found: $tName" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n--- Testing ServerRulesProfileManager ---" -ForegroundColor Cyan
$profileMgrType = $assembly.GetType("MCCheatChecker.ServerRulesProfileManager")
$curProfileProp = $profileMgrType.GetProperty("CurrentProfile", [System.Reflection.BindingFlags]"Public,Static")
$curDisplayNameProp = $profileMgrType.GetProperty("CurrentProfileDisplayName", [System.Reflection.BindingFlags]"Public,Static")

$curProfileVal = $curProfileProp.GetValue($null, $null)
$curDisplayName = $curDisplayNameProp.GetValue($null, $null)
Write-Host "Default Profile Enum Value: $curProfileVal" -ForegroundColor Yellow
Write-Host "Default Profile Display Name: $curDisplayName" -ForegroundColor Yellow

# Test Whitelist logic on CPVP mods
$isWhitelistedMethod = $profileMgrType.GetMethod("IsCpvpWhitelisted", [System.Reflection.BindingFlags]"Public,Static")
$cpvpTests = @(
    "MarlowCrystalOptimizer-1.2.jar",
    "HaramCrystal-v2.jar",
    "EndCrystalOptimizer.jar",
    "WalksyAnchorMacro.jar",
    "HerosCrystalOptimizer.jar",
    "CuteBowMod-1.20.jar"
)

foreach ($testName in $cpvpTests) {
    $wl = $isWhitelistedMethod.Invoke($null, @($testName))
    if ($wl -eq $true) {
        Write-Host "[PASS] CPVP Whitelisted: $testName" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Expected whitelisted: $testName" -ForegroundColor Red
        exit 1
    }
}

# Test Non-whitelisted cheat names
$isProhibitedMethod = $profileMgrType.GetMethod("IsProhibitedCheat", [System.Reflection.BindingFlags]"Public,Static")
$cheatTests = @(
    "baritone-api.jar",
    "freecam-mod.jar",
    "autototem-v1.jar",
    "killaura-helper.dll",
    "xray-fabric.jar"
)
foreach ($cheatName in $cheatTests) {
    $isCheat = $isProhibitedMethod.Invoke($null, @($cheatName))
    if ($isCheat -eq $true) {
        Write-Host "[PASS] Prohibited cheat correctly identified: $cheatName" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Prohibited cheat NOT recognized: $cheatName" -ForegroundColor Red
        exit 1
    }
}

# Test EvaluateModVerdict with Atlantis CPVP whitelist
Write-Host "`n--- Testing EvaluateModVerdict ---" -ForegroundColor Cyan
$evalMethod = $profileMgrType.GetMethod("EvaluateModVerdict", [System.Reflection.BindingFlags]"Public,Static")
$evalArgs1 = [object[]]@("marlow-crystal-optimizer", "marlow.jar", $null, $false, "", 0)
$verdict = $evalMethod.Invoke($null, $evalArgs1)
Write-Host "[PASS] Marlow Optimizer Verdict: $verdict" -ForegroundColor Green
if ($verdict -like "*РАЗРЕШЕННЫЙ CPVP ОПТИМИЗАТОР*") {
    Write-Host "[PASS] Atlantis CPVP Whitelist correctly applied in verdict evaluation!" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Verdict did not match expected whitelist!" -ForegroundColor Red
    exit 1
}

# Test EvaluateModVerdict with prohibited cheat
$evalArgs2 = [object[]]@("baritone", "baritone-1.20.jar", $null, $false, "", 0)
$verdict2 = $evalMethod.Invoke($null, $evalArgs2)
Write-Host "[PASS] Prohibited Mod Verdict: $verdict2" -ForegroundColor Green
if ($verdict2 -like "*ЗАПРЕЩЕННЫЙ ЧИТ-МОД*") {
    Write-Host "[PASS] Baritone flagged as prohibited cheat successfully!" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Baritone was not flagged as prohibited cheat!" -ForegroundColor Red
    exit 1
}

Write-Host "`n--- Testing ManualChecks Reflection ---" -ForegroundColor Cyan
$mcType = $assembly.GetType("MCCheatChecker.ManualChecks")

# Test RunMixinAndModJsonCheck
$mixinCheckMethod = $mcType.GetMethod("RunMixinAndModJsonCheck", [System.Reflection.BindingFlags]"Public,Static")
if ($mixinCheckMethod -ne $null) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $mixinResults = $mixinCheckMethod.Invoke($null, $null)
    $sw.Stop()
    Write-Host "[PASS] RunMixinAndModJsonCheck executed in $($sw.ElapsedMilliseconds) ms, returned $($mixinResults.Count) results" -ForegroundColor Green
} else {
    Write-Host "[FAIL] RunMixinAndModJsonCheck method not found!" -ForegroundColor Red
    exit 1
}

# Test RunUnifiedForensicTimelineCheck
$timelineCheckMethod = $mcType.GetMethod("RunUnifiedForensicTimelineCheck", [System.Reflection.BindingFlags]"Public,Static")
if ($timelineCheckMethod -ne $null) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $timelineResults = $timelineCheckMethod.Invoke($null, $null)
    $sw.Stop()
    Write-Host "[PASS] RunUnifiedForensicTimelineCheck executed in $($sw.ElapsedMilliseconds) ms, returned $($timelineResults.Count) results" -ForegroundColor Green
} else {
    Write-Host "[FAIL] RunUnifiedForensicTimelineCheck method not found!" -ForegroundColor Red
    exit 1
}

Write-Host "`n--- Testing Form Instantiation ---" -ForegroundColor Cyan
$mixinFormType = $assembly.GetType("MCCheatChecker.MixinAndModJsonForensicsViewerForm")
$mixinForm = [System.Activator]::CreateInstance($mixinFormType, @("TestPlayer"))
if ($mixinForm -ne $null) {
    Write-Host "[PASS] MixinAndModJsonForensicsViewerForm instantiated successfully: $($mixinForm.Text)" -ForegroundColor Green
    $mixinForm.Dispose()
} else {
    Write-Host "[FAIL] MixinAndModJsonForensicsViewerForm instantiation failed!" -ForegroundColor Red
    exit 1
}

$timelineFormType = $assembly.GetType("MCCheatChecker.UnifiedForensicTimelineViewerForm")
$timelineForm = [System.Activator]::CreateInstance($timelineFormType, @("TestPlayer"))
if ($timelineForm -ne $null) {
    Write-Host "[PASS] UnifiedForensicTimelineViewerForm instantiated successfully: $($timelineForm.Text)" -ForegroundColor Green
    $timelineForm.Dispose()
} else {
    Write-Host "[FAIL] UnifiedForensicTimelineViewerForm instantiation failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n[+++] ALL PHASE 7 EVASION SUITE TESTS PASSED 100%! [+++]" -ForegroundColor Green
